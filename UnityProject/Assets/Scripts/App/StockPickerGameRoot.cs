using StockPicker.Game.Core;
using UnityEngine;

namespace StockPicker.App
{
    /// <summary>
    /// Scene entry: holds serialized rules, wires flow + HUD + 3D board.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StockPickerGameRoot : MonoBehaviour
    {
        [SerializeField] private GameRules _rulesAsset;
        [SerializeField] private int _seasonsPerCampaign;

        private void Awake()
        {
            var flow = GetComponent<GameFlowController>();
            if (flow == null)
                flow = gameObject.AddComponent<GameFlowController>();
            flow.Initialize(_rulesAsset, _seasonsPerCampaign);

            if (GetComponent<GameHudView>() == null)
                gameObject.AddComponent<GameHudView>();
            if (GetComponent<BoardWorldPresenter>() == null)
                gameObject.AddComponent<BoardWorldPresenter>();
            if (GetComponent<DiceTrayPresenter>() == null)
                gameObject.AddComponent<DiceTrayPresenter>();

            flow.TryLoadOrNew();
        }
    }
}
