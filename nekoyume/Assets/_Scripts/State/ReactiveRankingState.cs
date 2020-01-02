using UniRx;

namespace Nekoyume.State
{
    /// <summary>
    /// RankingState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveRankingState
    {
        public static readonly ReactiveProperty<RankingState> RankingState = new ReactiveProperty<RankingState>();

        public static void Initialize(RankingState rankingState)
        {
            if (ReferenceEquals(rankingState, null))
            {
                return;
            }

            RankingState.Value = rankingState;
        }
    }
}