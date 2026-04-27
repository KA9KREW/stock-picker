using StockPicker.Game.Core;
using StockPicker.Infrastructure.Backend;
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
        [Tooltip("Must be OFF for Google Play production builds (real PlayFab + Google Sign-In).")]
        [SerializeField] private bool _useLocalMockBackend = false;
        [Tooltip("Set PlayFab Title ID and Google OAuth Web Client ID before release.")]
        [SerializeField] private BackendConfig _backendConfig = new();

        private void Awake()
        {
            var flow = GetComponent<GameFlowController>();
            if (flow == null)
                flow = gameObject.AddComponent<GameFlowController>();
            flow.Initialize(_rulesAsset, _seasonsPerCampaign, _backendConfig, _useLocalMockBackend);

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
