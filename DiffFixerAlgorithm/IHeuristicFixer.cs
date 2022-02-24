using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public interface IHeuristicFixer
    {
        FixAction RunFixHeuristic
        (
            List<YamlQuery> queryChain, 
            string newValue, 
            YamlStream newYamlStream, 
            YamlStream oldYamlStream
        );


        public enum FixActionType
        {
            None = 0,
            RevertToOldValue
        }

        public abstract class FixAction
        {
            public FixActionType ActionType { get; private set; }

            public FixAction(FixActionType actionType)
            {
                ActionType = actionType;
            }
        }

        public class FixActionNone : FixAction
        {
            public FixActionNone() : base(FixActionType.None)
            { 
            }
        }

        public class FixActionChangeValue : FixAction
        {
            public string ValueToUse { get; private set; }

            public FixActionChangeValue(string valueToUse) : base(FixActionType.RevertToOldValue)
            {
                ValueToUse = valueToUse;
            }
        }
    }
    
}
