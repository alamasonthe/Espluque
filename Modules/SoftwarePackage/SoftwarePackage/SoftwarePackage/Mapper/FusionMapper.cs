using Espluque.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace SoftwarePackage.Mapper
{
    public static class FusionMapper<TEntity> where TEntity : new()
    {
        
        public static TEntity Map(IAnalysisContext analysisContext, List<MapLine> mappings, Espluque.Contracts.Ports.ILogger logger)
        {
            var formattedFilename = Path.GetFileName(analysisContext.FilePath).PadRight(35);
            if (analysisContext is null || mappings is null)
            {
                logger.Log(LogLevel.Error, $"{formattedFilename}\tPackage mapping failed: Analyze context or mappings are null");
                return new TEntity();
            }

            TEntity entity = new();

            List<PropertyInfo> properties = typeof(TEntity).GetProperties().ToList();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                List<MapLine> matchingMappings = mappings.Where(x => x.TargetPropertyName == propertyName).ToList();

                    foreach (var mapLine in matchingMappings)
                    {
                    try
                    {
                        var grabberResult = analysisContext.ObservedData.SingleOrDefault(x =>
                            x.ModuleName == mapLine.ModuleName &&
                            x.ContributionLabel == mapLine.ContributionLabel);

                        if (grabberResult is null) continue;

                        var value = grabberResult.GrabbedInformation.SingleOrDefault(x =>
                            x.Key == mapLine.GrabbedInformationKey).Value;

                        if (value is null) continue;

                        property.SetValue(entity, value);
                        logger.Log(LogLevel.Debug, $"{formattedFilename}\tMapped property '{propertyName}' with value '{value}' from module '{mapLine.ModuleName}' and contribution label '{mapLine.ContributionLabel}'.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Log(LogLevel.Error, $"{formattedFilename}\tMapping failed for property '{propertyName}' from module '{mapLine.ModuleName}' and contribution label '{mapLine.ContributionLabel}': {ex.Message}");
                    }
                }
            }
            return entity;
        }
    }
}