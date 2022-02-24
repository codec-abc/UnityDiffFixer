using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityDiffFixer;
using YamlDotNet.RepresentationModel;
using static UnityDiffFixer.IHeuristicFixer;

namespace DiffFixerAlgorithm.FixHeuristics
{
    public class SamePropComparer : IHeuristicFixer
    {
        private INumberComparer m_comparer;

        public SamePropComparer(INumberComparer comparer)
        {
            m_comparer = comparer;
        }
    
        public FixAction RunFixHeuristic(List<YamlQuery> queryChain, string newValue, YamlStream newYamlStream, YamlStream oldYamlStream)
        {
            try
            {
                if (queryChain.Any(a => a.QueryType == YamlQueryType.ByIndex))
                {
                    return new FixActionNone();
                }

                var queryResult = YamlQueryUtils.RunQueryChain(queryChain, oldYamlStream);

                if (queryResult == null)
                {
                    return new FixActionNone();
                }

                string oldValue = (string)queryResult;
                if (oldValue == newValue)
                {
                    return new FixActionNone();
                }

                double oldValueAsDouble;
                if (double.TryParse(oldValue, NumberStyles.Any, CultureInfo.InvariantCulture, out oldValueAsDouble))
                {
                    double newValueAsDouble;
                    if (double.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out newValueAsDouble))
                    {
                        if (m_comparer.AreSame(oldValueAsDouble, newValueAsDouble))
                        {
                            return new FixActionChangeValue(oldValue);
                        }
                    }
                }
                return new FixActionNone();
            }
            catch (Exception)
            {
                //TODO LOG Error
                return new FixActionNone();
            }

        }
    }
}
