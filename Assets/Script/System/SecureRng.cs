using System;
using System.Collections.Generic;
using System.Security,Cryptography;

public static class SecureRng {
    public static int Range(int minInclusive, int maxExclusive) 
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);

    public static float Value01() {
        int r = RandomNumberGenerator.GetInt32(0, int.MaxValue);
        return r / (float)int.MaxValue;
    }

    public static bool Chance(float p) {
        if(p <= 0f) return false;
        if(p >= 1f) return true;
        return Value01() < p;
    }

    public static T Weighted<T>(IReadOnlyList<(T item, float weight)> items) {
        if(items == null || items.Count == 0) throw new ArgumentException("empty");
        double sum = 0;
        foreach(var (_, w) in items) {
            if (w <= 0) continue;
            sum += w;
        }

        if(sum <= 0) throw new ArgumentException("all weights <= 0");

        double pick = Value01() * sum;
        double acc = 0;
        foreach(var (it, w) in items) {
            if(w <= 0) continue;
            acc += w;
            if(pick < acc) return it;
        }

        return items[^1].item;
    }
}