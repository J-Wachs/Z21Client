using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Logging messages use localized resource strings.")]
[assembly: SuppressMessage("Performance", "CA1873:Avoid unnecessary argument evaluation in logging", Justification = "Resource property access is required for localization.")]
