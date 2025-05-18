using System.Collections.Generic;

namespace HomeDecorator.Core.Services;

/// <summary>
/// Service to manage feature flags throughout the application
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Gets whether the application is running in fake data mode
    /// </summary>
    bool IsFakeDataMode { get; }

    /// <summary>
    /// Gets the value of a feature flag
    /// </summary>
    /// <param name="flagName">The name of the flag to retrieve</param>
    /// <param name="defaultValue">Default value if the flag is not defined</param>
    /// <returns>The value of the feature flag</returns>
    bool GetFlag(string flagName, bool defaultValue = false);
}

/// <summary>
/// Default implementation of IFeatureFlagService
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly Dictionary<string, bool> _flags; public FeatureFlagService(Dictionary<string, bool>? initialFlags = null)
    {
        _flags = initialFlags ?? new Dictionary<string, bool>();

        // Ensure the fake data mode flag is always initialized
        if (!_flags.ContainsKey("IsFakeDataMode"))
        {
            _flags["IsFakeDataMode"] = false;
        }
    }

    /// <inheritdoc />
    public bool IsFakeDataMode => GetFlag("IsFakeDataMode");

    /// <inheritdoc />
    public bool GetFlag(string flagName, bool defaultValue = false)
    {
        return _flags.TryGetValue(flagName, out bool value) ? value : defaultValue;
    }
}
