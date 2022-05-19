using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityDiffFixer;
using YamlDotNet.RepresentationModel;

namespace UnityAssetPropertyChanger
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootDir = @"C:\Workplace\repos\BTP_2\Assets\_PROJECT\0DAsset\Resources\Module";
            var files = Directory.EnumerateFiles(rootDir, "*.asset", SearchOption.AllDirectories);
            Console.WriteLine($"starting");
            foreach (var file in files)
            {
                try
                {
                    if (!file.EndsWith(".asset"))
                    {
                        continue;
                    }

                    var hasDiff = false;
                    string content = File.ReadAllText(file);


                    if (!content.StartsWith("%YAML 1.1"))
                    {
                        continue;
                    }

                    Console.WriteLine($"processing {file}");

                    var lines = StringUtils.GetAllLinesFromText(content);
                    var document = UnityYAMLDocument.ParseUnityYAMLdocument(lines);

                    var queriesByComponents = document.GetQueriesByComponent();

                    foreach (var kvp in queriesByComponents)
                    {
                        var unityComponentIdentifier = kvp.Key;
                        var yamlStream = document.GetYamlStreamForComponent(unityComponentIdentifier);
                        var node = YamlQueryUtils.RunQueryChain(GetScriptGuidQueryChain(), yamlStream);
                        if (node == null)
                        {
                            continue;
                        }
                        var nodeContent = (string)node;

                        if (nodeContent == "3dbc5de9c8ba97a4b884f02c5cb18cb0")
                        {
                            hasDiff = true;
                            var useBTPColorNode = YamlQueryUtils.RunQueryChain(GetUseBTPColorQueryChain(), yamlStream);
                            ((YamlScalarNode)useBTPColorNode).Value = "1";

                            var unfinishOnExitNode = YamlQueryUtils.RunQueryChain(GetUnfinishOnExitQueryChain(), yamlStream);
                            ((YamlScalarNode)unfinishOnExitNode).Value = "1";

                            var endColorNodeR = YamlQueryUtils.RunQueryChain(GetEndColorRQueryChain(), yamlStream);
                            ((YamlScalarNode)endColorNodeR).Value = "1";

                            var endColorNodeG = YamlQueryUtils.RunQueryChain(GetEndColorGQueryChain(), yamlStream);
                            ((YamlScalarNode)endColorNodeG).Value = "0.87058824";

                            var endColorNodeB = YamlQueryUtils.RunQueryChain(GetEndColorBQueryChain(), yamlStream);
                            ((YamlScalarNode)endColorNodeB).Value = "0";

                            var endColorNodeA = YamlQueryUtils.RunQueryChain(GetEndColorAQueryChain(), yamlStream);
                            ((YamlScalarNode)endColorNodeA).Value = "1";
                        }
                    }

                    if (hasDiff)
                    {
                        var fixedDoc = YamlQueryUtils.ReserializeNewDocWithFix(document);
                        Console.WriteLine($"Fixing {file}");
                        File.WriteAllText(file, fixedDoc);
                    }
                } 
                catch(Exception e)
                {
                    Console.WriteLine($"Error while processing file {file}: {e.Message}");
                }
            }
            return 0;
        }

        private static List<YamlQuery> GetScriptGuidQueryChain()
        {
            var root = new YamlDummyQuery();
            var monoBehaviourQuery = new YamlQueryByName(root, "MonoBehaviour");
            var scriptQuery = new YamlQueryByName(monoBehaviourQuery, "m_Script");
            var guid = new YamlQueryByName(scriptQuery, "guid");

            return new List<YamlQuery>()
            {
                root,
                monoBehaviourQuery,
                scriptQuery,
                guid
            };
        }

        private static List<YamlQuery> GetUnfinishOnExitQueryChain()
        {
            var root = new YamlDummyQuery();
            var monoBehaviourQuery = new YamlQueryByName(root, "MonoBehaviour");
            var unfinishOnExit = new YamlQueryByName(monoBehaviourQuery, "m_unfinishOnExit");

            return new List<YamlQuery>()
            {
                root,
                monoBehaviourQuery,
                unfinishOnExit
            };
        }

        private static List<YamlQuery> GetUseBTPColorQueryChain()
        {
            var root = new YamlDummyQuery();
            var monoBehaviourQuery = new YamlQueryByName(root, "MonoBehaviour");
            var useBTPColor = new YamlQueryByName(monoBehaviourQuery, "m_UseBTPColor");

            return new List<YamlQuery>()
            {
                root,
                monoBehaviourQuery,
                useBTPColor
            };
        }

        private static List<YamlQuery> GetEndColorQueryChain()
        {
            var root = new YamlDummyQuery();
            var monoBehaviourQuery = new YamlQueryByName(root, "MonoBehaviour");
            var endColor = new YamlQueryByName(monoBehaviourQuery, "endColor");

            return new List<YamlQuery>()
            {
                root,
                monoBehaviourQuery,
                endColor
            };
        }

        private static List<YamlQuery> GetEndColorRQueryChain()
        {
            var colorNodeQueryChain = GetEndColorQueryChain();
            colorNodeQueryChain.Add(new YamlQueryByName(colorNodeQueryChain.Last(), "r"));
            return colorNodeQueryChain;
        }

        private static List<YamlQuery> GetEndColorGQueryChain()
        {
            var colorNodeQueryChain = GetEndColorQueryChain();
            colorNodeQueryChain.Add(new YamlQueryByName(colorNodeQueryChain.Last(), "g"));
            return colorNodeQueryChain;
        }

        private static List<YamlQuery> GetEndColorBQueryChain()
        {
            var colorNodeQueryChain = GetEndColorQueryChain();
            colorNodeQueryChain.Add(new YamlQueryByName(colorNodeQueryChain.Last(), "b"));
            return colorNodeQueryChain;
        }

        private static List<YamlQuery> GetEndColorAQueryChain()
        {
            var colorNodeQueryChain = GetEndColorQueryChain();
            colorNodeQueryChain.Add(new YamlQueryByName(colorNodeQueryChain.Last(), "a"));
            return colorNodeQueryChain;
        }
    }
}
