export async function estimatePrice(body) {
  const res = await fetch('/api/CarPricePrediction/estimate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Request failed: ${res.status} ${text}`);
  }

  const json = await res.json();
  return json;
}
