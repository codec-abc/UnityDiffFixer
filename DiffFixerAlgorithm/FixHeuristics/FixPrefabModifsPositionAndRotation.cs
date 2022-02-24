using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityDiffFixer;
using YamlDotNet.RepresentationModel;
using static UnityDiffFixer.IHeuristicFixer;

namespace DiffFixerAlgorithm.FixHeuristics
{
    public class FixPrefabModifsPositionAndRotation : IHeuristicFixer
    {
        private List<INumberComparer> m_NumberComparers = new List<INumberComparer>();

        public FixPrefabModifsPositionAndRotation(List<INumberComparer> numberComparers)
        {
            m_NumberComparers = numberComparers;
        }

        public FixAction RunFixHeuristic
       (
           List<YamlQuery> queryChain,
           string newValue,
           YamlStream newYamlStream,
           YamlStream oldYamlStream
       )
        {
            try
            {
                if 
                (
                    queryChain.Count == 6 &&
                    queryChain[1].QueryType == YamlQueryType.ByName && queryChain[1].AsNameQuery().Name == "PrefabInstance" &&
                    queryChain[2].QueryType == YamlQueryType.ByName && queryChain[2].AsNameQuery().Name == "m_Modification" &&
                    queryChain[3].QueryType == YamlQueryType.ByName && queryChain[3].AsNameQuery().Name == "m_Modifications" &&
                    queryChain[4].QueryType == YamlQueryType.ByIndex &&
                    queryChain[5].QueryType == YamlQueryType.ByName && queryChain[5].AsNameQuery().Name == "value"
                )
                {
                    var target = queryChain.GetRange(0, queryChain.Count - 1);

                    var indexedNode = (YamlMappingNode)YamlQueryUtils.RunQueryChain(target, newYamlStream);

                    var targetNode = (YamlMappingNode)indexedNode["target"];
                    var newPropertyPath = (string)indexedNode["propertyPath"];
                    var newTargetFileID = (string)targetNode["fileID"];
                    var newTargetGUID = (string)targetNode["guid"];
                    
                    var ancestor = queryChain.GetRange(0, queryChain.Count - 2);
                    var modificationsChildNode = (YamlSequenceNode) YamlQueryUtils.RunQueryChain(ancestor, oldYamlStream);

                    foreach (var childNode in modificationsChildNode.Children)
                    {
                        if (childNode.NodeType == YamlNodeType.Mapping)
                        {
                            var childMappingNode = (YamlMappingNode)childNode;
                            var oldValue = (string)childMappingNode["value"];
                            var oldPropertyPath = (string)childMappingNode["propertyPath"];
                            var oldTargetNode = childMappingNode["target"];
                            var oldTargetFileID = (string)oldTargetNode["fileID"];
                            var oldTargetGUID = (string)oldTargetNode["guid"];
                            if 
                            (
                                newPropertyPath == oldPropertyPath &&
                                newTargetFileID == oldTargetFileID &&
                                newTargetGUID == oldTargetGUID
                            )
                            {
                                if (oldValue != newValue)
                                {
                                    double oldValueAsDouble;
                                    if (double.TryParse(oldValue, NumberStyles.Any, CultureInfo.InvariantCulture, out oldValueAsDouble))
                                    {
                                        double newValueAsDouble;
                                        if (double.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out newValueAsDouble))
                                        {

                                            var areSame = m_NumberComparers
                                                .Select(a => a.AreSame(oldValueAsDouble, newValueAsDouble))
                                                .Aggregate(false, (accum, boolVal) => accum || boolVal);

                                            if (areSame)
                                            {
                                                return new FixActionChangeValue(oldValue);
                                            }
                                        }
                                    }
                                }
                            }
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
