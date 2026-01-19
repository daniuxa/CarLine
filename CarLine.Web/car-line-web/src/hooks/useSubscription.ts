import { useState } from 'react';

import { createCarSubscription } from '../services/subscription-service';

/**
 * Custom hook for managing car subscription operations
 * @returns {Object} Hook state and methods
 */
export const useSubscription = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [isError, setIsError] = useState(false);
  const [error, setError] = useState(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [data, setData] = useState(null);

  /**
   * Submit a new car subscription
   * @param {Object} subscriptionData - The subscription configuration
   */
  const submitSubscription = async (subscriptionData) => {
    setIsLoading(true);
    setIsError(false);
    setError(null);
    setIsSuccess(false);
    setData(null);

    try {
      const result = await createCarSubscription(subscriptionData);
      setData(result);
      setIsSuccess(true);
      return result;
    } catch (err) {
      setIsError(true);
      setError(err.message || 'Failed to create subscription');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Reset the hook state
   */
  const reset = () => {
    setIsLoading(false);
    setIsError(false);
    setError(null);
    setIsSuccess(false);
    setData(null);
  };

  return {
    submitSubscription,
    reset,
    isLoading,
    isError,
    error,
    isSuccess,
    data,
  };
};
