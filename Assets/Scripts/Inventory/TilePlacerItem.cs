using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Tile Placer", menuName = "Item/Right Clickable/Tile Placer")]
public class TilePlacerItem : ItemClass
{
    public Tile tile;
    
    public override TilePlacerItem GetTilePlacer() { return this; }
    protected override void Use(PlayerController caller) { }

    public override void RightClick(PlayerController caller, Vector3Int tilePosition)
    {
        caller.world.collidableTilemap.SetTile(tilePosition, null); // remove ocean tiles
        caller.world.triggerTilemap.SetTile(tilePosition, tile); // set new tile
    }
}
