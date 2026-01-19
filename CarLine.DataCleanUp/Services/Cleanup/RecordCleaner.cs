using CarLine.DataCleanUp.Models;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal static class RecordCleaner
{
    public static bool CleanAndValidate(Dictionary<string, string> csvRecord, Dictionary<string, string> fullRecord)
    {
        // Validate required categorical fields - skip record if invalid
        if (!ValidateField(csvRecord, "transmission", DataCleanupConstants.ValidTransmissions))
            return false;
        if (csvRecord.TryGetValue("transmission", out var trans))
            fullRecord["transmission"] = trans;

        if (!ValidateField(csvRecord, "condition", DataCleanupConstants.ValidConditions))
            return false;
        if (csvRecord.TryGetValue("condition", out var cond))
            fullRecord["condition"] = cond;

        if (!ValidateField(csvRecord, "fuel", DataCleanupConstants.ValidFuels))
            return false;
        if (csvRecord.TryGetValue("fuel", out var fuel))
            fullRecord["fuel"] = fuel;

        if (!ValidateField(csvRecord, "type", DataCleanupConstants.ValidTypes))
            return false;
        if (csvRecord.TryGetValue("type", out var type))
            fullRecord["type"] = type;

        // Standardize manufacturer
        if (csvRecord.TryGetValue("manufacturer", out var manu) && !string.IsNullOrWhiteSpace(manu))
        {
            manu = manu.Trim().ToLowerInvariant();
            if (DataCleanupConstants.ManufacturerMap.TryGetValue(manu, out var standardized))
            {
                csvRecord["manufacturer"] = standardized;
                fullRecord["manufacturer"] = standardized;
            }
            else
            {
                csvRecord["manufacturer"] = manu;
                fullRecord["manufacturer"] = manu;
            }
        }
        else
        {
            return false;
        }

        // Model normalization
        if (csvRecord.TryGetValue("model", out var model) && !string.IsNullOrWhiteSpace(model))
        {
            var normalized = NormalizeModelName(model);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;
            csvRecord["model"] = normalized;
            fullRecord["model"] = normalized;
        }
        else
        {
            return false;
        }

        // Year
        if (csvRecord.TryGetValue("year", out var yearStr) && !string.IsNullOrWhiteSpace(yearStr))
        {
            if (int.TryParse(yearStr, out var year))
            {
                if (year < 1900 || year > DateTime.UtcNow.Year + 1)
                    return false;
                csvRecord["year"] = year.ToString();
                fullRecord["year"] = year.ToString();
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        // Price
        if (csvRecord.TryGetValue("price", out var priceStr) && !string.IsNullOrWhiteSpace(priceStr))
        {
            if (decimal.TryParse(priceStr, out var price))
            {
                if (price < 100 || price > 1000000)
                    return false;
                var normalized = price.ToString("F0");
                csvRecord["price"] = normalized;
                fullRecord["price"] = normalized;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        // Odometer
        if (csvRecord.TryGetValue("odometer", out var odoStr) && !string.IsNullOrWhiteSpace(odoStr))
        {
            if (int.TryParse(odoStr, out var odo))
            {
                if (odo < 0 || odo > 500000)
                    return false;

                fullRecord["odometer"] = odo.ToString();
                csvRecord["odometer"] = Math.Log10(odo + 1).ToString("F3");
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        // Optional region
        if (csvRecord.TryGetValue("region", out var region) && !string.IsNullOrWhiteSpace(region))
        {
            var normalized = region.Trim().ToLowerInvariant();
            csvRecord["region"] = normalized;
            fullRecord["region"] = normalized;
        }

        return true;
    }

    private static bool ValidateField(Dictionary<string, string> record, string fieldName, HashSet<string> validValues)
    {
        if (!record.TryGetValue(fieldName, out var value))
            return false;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLowerInvariant();

        if (!validValues.Contains(value))
            return false;

        record[fieldName] = value;
        return true;
    }

    /// <summary>
    ///     Normalize model name by taking only the first word.
    /// </summary>
    private static string NormalizeModelName(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return string.Empty;

        model = model.Trim().ToLowerInvariant();

        var firstWord = model.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return firstWord ?? string.Empty;
    }
}