using DiffFixerAlgorithm;
using DiffFixerAlgorithm.FixHeuristics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using static UnityDiffFixer.IHeuristicFixer;

namespace UnityDiffFixer
{
    public partial class UnityDiffComparerAndFixer
    {
        private readonly string[] FORBIDDEN_NAMES = { "guid", "fileID" };

        private UnityYAMLDocument m_oldDoc;
        private UnityYAMLDocument m_newDoc;
        public DiffOptions DiffOptions { get; }

        private int m_nbReverted = 0;

        public UnityDiffComparerAndFixer(UnityYAMLDocument oldDoc, UnityYAMLDocument newDoc)
        {
            m_oldDoc = oldDoc;
            m_newDoc = newDoc;
        }

        public UnityDiffComparerAndFixer
        (
            UnityYAMLDocument oldDoc,
            UnityYAMLDocument newDoc,
            DiffOptions diffOptions
        ) : this(oldDoc, newDoc)
        {
            DiffOptions = diffOptions;
        }

        public List<IHeuristicFixer> m_HeuristicFixes = new List<IHeuristicFixer>()
        {
            new SamePropComparer(new CloseToZero()),
            new SamePropComparer(new SmallPercentDifference()),
            new SamePropComparer(new TinyAbsoluteDiff()),
            new FixPrefabModifsPositionAndRotation
            (
                new List<INumberComparer>
                {
                    new CloseToZero(),
                    new SmallPercentDifference(),
                    new TinyAbsoluteDiff()
                }
            )
        };

        public string GetFixedVersion(Action<string> printAction)
        {
            var queriesByComponents = m_newDoc.GetQueriesByComponent();

            foreach (var kvp in queriesByComponents)
            {
                var unityComponentIdentifier = kvp.Key;

                if (m_oldDoc.HasComponent(unityComponentIdentifier))
                {
                    var oldYamlStream = m_oldDoc.GetYamlStreamForComponent(unityComponentIdentifier);
                    var newYamlStream = m_newDoc.GetYamlStreamForComponent(unityComponentIdentifier);
                    var queries = kvp.Value;

                    foreach (var query in queries)
                    {
                        try
                        {
                            var queryList = YamlQueryUtils.GetQueryChainFromRootNode(query);
                            var newValue = ((string)YamlQueryUtils.RunQueryChain(queryList, newYamlStream));
                            var queryChain = YamlQueryUtils.GetQueryChainFromRootNode(query);

                            var actionToDo = m_HeuristicFixes
                                .Select(a => a.RunFixHeuristic(queryList, newValue, newYamlStream, oldYamlStream))
                                .Where(a => a.ActionType != FixActionType.None)
                                .FirstOrDefault();

                            if (actionToDo == null)
                            {
                                continue;
                            }

                            switch (actionToDo.ActionType)
                            {
                                case FixActionType.RevertToOldValue:
                                    var oldValue = ((FixActionChangeValue)actionToDo).ValueToUse;
                                    m_nbReverted += 1;

                                    var revertStatus = RevertValue(query, oldValue, newYamlStream);

                                    if (DiffOptions.ShouldPrintToConsole && revertStatus == RevertStatus.Reverted)
                                    {
                                        var debug = "";

                                        foreach (var currentQuery in queryList)
                                        {
                                            debug += currentQuery.ToString();
                                        }

                                        printAction
                                        (
                                            "reverting comp " + unityComponentIdentifier.ToString() + " props " + debug
                                            + " from " + newValue + " to " + oldValue
                                        );
                                    }
                                    break;

                                case FixActionType.None:
                                    break;

                            }

                        }
                        catch (Exception e)
                        {
                            if (DiffOptions.ShouldPrintToConsole)
                            {
                                printAction("Error " + e.Message);
                            }
                        }
                    }
                }
            }

            if (DiffOptions.ShouldPrintToConsole)
            {
                printAction("Numbers of reverted values " + m_nbReverted);
            }

            return ReserializeNewDocWithFix(printAction);
        }

        private string ReserializeNewDocWithFix(Action<string> printAction = null)
        {
            return YamlQueryUtils.ReserializeNewDocWithFix(m_newDoc, printAction);
        }

        public enum RevertStatus
        {
            Reverted,
            ForbiddenElement
        }

        private RevertStatus RevertValue(YamlQuery query, string oldValue, YamlStream newYamlStream)
        {
            List<YamlQuery> queryList = YamlQueryUtils.GetQueryChainFromRootNode(query);
            var terminalNode = YamlQueryUtils.RunQueryChain(queryList, newYamlStream);

            var isForbiddenName = false;
            var lastQueryElem = queryList.Last();
            if (lastQueryElem is YamlQueryByName)
            {
                var name = (lastQueryElem as YamlQueryByName).Name;
                foreach (var forbiddenName in FORBIDDEN_NAMES)
                {
                    if (forbiddenName == name)
                    {
                        isForbiddenName = true;
                        break;
                    }
                }
            }

            if (!isForbiddenName)
            {
                var realNode = (YamlScalarNode)terminalNode;
                realNode.Value = oldValue;

                return RevertStatus.Reverted;
            } 
            else
            {
                return RevertStatus.ForbiddenElement;
            }
        }

    }
}
