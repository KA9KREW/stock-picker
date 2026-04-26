StockTickerBoard.obj — licensed board mesh (same source as uploads_files_*_Stock+Ticker+Board.obj at repo root).

Unity imports the .obj + StockTickerBoard.mtl automatically. In Play Mode, BoardWorldPresenter resolves this model from:
  • Inspector: drag StockTickerBoard.obj onto "Imported Board Model", or
  • Editor default: Assets/Art/Board/StockTickerBoard.obj (auto-loaded if the field is empty), or
  • Builds: Resources/Board/StockTickerBoard (optional copy) if you do not assign the reference in a prefab/scene.

Vintage mode defaults to ImportedModel: the mesh is the visible board; tokens/highlights still use ClassicStockBoard layout math (tune scale/position on BoardWorldPresenter if pawns do not line up).

Use Vintage Board Visual = Procedural Classic for the old all-quad board, or Procedural With Imported Underlay to keep the mesh behind the procedural layer.

If units look huge, lower Board Mesh Scale (e.g. 0.05) and nudge Board Mesh Local Position / Euler.
