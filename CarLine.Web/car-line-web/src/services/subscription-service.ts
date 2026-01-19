/**
 * Subscribe to notifications for cars matching specific criteria
 * @param {Object} subscriptionData - The subscription configuration
 * @param {string} subscriptionData.email - User email for notifications
 * @param {string} subscriptionData.manufacturer - Car manufacturer
 * @param {string} subscriptionData.model - Car model
 * @param {number} subscriptionData.yearFrom - Minimum year
 * @param {number} subscriptionData.yearTo - Maximum year
 * @param {number} subscriptionData.odometerFrom - Minimum odometer reading
 * @param {number} subscriptionData.odometerTo - Maximum odometer reading
 * @param {string} subscriptionData.condition - Car condition
 * @param {string} subscriptionData.fuel - Fuel type
 * @param {string} subscriptionData.transmission - Transmission type
 * @param {string} subscriptionData.type - Vehicle type
 * @param {string} subscriptionData.region - Region
 * @returns {Promise<Object>} Response with subscription details
 */
export const createCarSubscription = async (subscriptionData) => {
  try {
    const response = await fetch(`api/carSubscription`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(subscriptionData),
    });

    if (!response.ok) {
      const bodyText = await response.text();
      let message = bodyText || 'Failed to create subscription';
      try {
        const errorData = bodyText ? JSON.parse(bodyText) : null;
        message = errorData?.message || errorData?.title || message;
      } catch {
        // ignore non-JSON error body
      }
      throw new Error(message);
    }

    return await response.json();
  } catch (error) {
    console.error('Error creating subscription:', error);
    throw error;
  }
};

/**
 * Get all subscriptions for a user
 * @param {string} email - User email
 * @returns {Promise<Array>} List of subscriptions
 */
export const getUserSubscriptions = async (email) => {
  try {
    const response = await fetch(`api/carSubscription?email=${encodeURIComponent(email)}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const bodyText = await response.text();
      let message = bodyText || 'Failed to fetch subscriptions';
      try {
        const errorData = bodyText ? JSON.parse(bodyText) : null;
        message = errorData?.message || errorData?.title || message;
      } catch {
        // ignore non-JSON error body
      }
      throw new Error(message);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching subscriptions:', error);
    throw error;
  }
};

/**
 * Delete a subscription
 * @param {string} subscriptionId - The subscription ID to delete
 * @returns {Promise<Object>} Response confirming deletion
 */
export const deleteSubscription = async (subscriptionId) => {
  try {
    const response = await fetch(`api/carSubscription/${subscriptionId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const bodyText = await response.text();
      let message = bodyText || 'Failed to delete subscription';
      try {
        const errorData = bodyText ? JSON.parse(bodyText) : null;
        message = errorData?.message || errorData?.title || message;
      } catch {
        // ignore non-JSON error body
      }
      throw new Error(message);
    }

    // API returns 204 No Content on success
    if (response.status === 204) return;

    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) return await response.json();
    return await response.text();
  } catch (error) {
    console.error('Error deleting subscription:', error);
    throw error;
  }
};
