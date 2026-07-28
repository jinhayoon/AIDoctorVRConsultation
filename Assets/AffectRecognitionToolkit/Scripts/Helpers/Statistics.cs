using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Statistics
{
    // Function to compute the mean of a list of float values
    public static float ComputeMean(List<float> values)
    {
        if (values.Count == 0)
            return 0f;
        return values.Sum() / values.Count;
    }

    // Function to compute the median of a list of float values
    public static float ComputeMedian(List<float> values)
    {
        int count = values.Count;
        if (count == 0)
            return 0f;

        values.Sort();

        if (count % 2 == 0)
        {
            int midIndex = count / 2;
            return (values[midIndex - 1] + values[midIndex]) / 2f;
        }
        else
        {
            int midIndex = count / 2;
            return values[midIndex];
        }
    }

    // Function to compute the standard deviation of a list of float values
    public static float ComputeStandardDeviation(List<float> values)
    {
        int count = values.Count;
        if (count == 0)
            return 0f;

        float mean = ComputeMean(values);
        float sumOfSquaredDifferences = values.Sum(x => (x - mean) * (x - mean));
        return (float)Math.Sqrt(sumOfSquaredDifferences / count);
    }
}
