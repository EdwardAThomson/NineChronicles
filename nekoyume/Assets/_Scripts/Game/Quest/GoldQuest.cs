using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    public class GoldQuest : Quest
    {
        public readonly TradeType Type;
        private decimal _current;

        public GoldQuest(GoldQuestSheet.Row data) : base(data)
        {
            Type = data.Type;
        }

        public GoldQuest(Dictionary serialized) : base(serialized)
        {
            _current = serialized[(Text) "current"].ToDecimal();
            Type = (TradeType) (int) ((Integer) serialized[(Bencodex.Types.Text) "type"]).Value;
        }

        public override QuestType QuestType => QuestType.Exchange;

        public override void Check()
        {
            Complete = _current >= Goal;
        }

        public override string ToInfo()
        {
            return string.Format(GoalFormat, GetName(), _current, Goal);
        }

        public override string GetName()
        {
            return string.Format(LocalizationManager.Localize($"QUEST_GOLD_{Type}_FORMAT"), Goal);
        }

        protected override string TypeId => "GoldQuest";

        public void Update(decimal gold)
        {
            _current += gold;
            Check();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = _current.Serialize(),
                [(Text) "type"] = (Integer) (int) Type,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}