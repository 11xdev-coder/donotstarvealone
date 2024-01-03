using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Tile Placer", menuName = "Item/Right Clickable/Tile Placer")]
public class TilePlacerItem : ItemClass
{
    [Header("Tile placer")]
    public Tile tile;
    
    public override TilePlacerItem GetTilePlacer() { return this; }
    protected override void Use(PlayerController caller) { }

    // ReSharper disable Unity.PerformanceAnalysis
    public override void RightClick(PlayerController caller, Vector3Int tilePosition)
    {
        caller.world.collidableTilemap.SetTile(tilePosition, null); // remove ocean tiles
        caller.world.triggerTilemap.SetTile(tilePosition, tile); // set new tile

        caller.inventory.Remove(this, 1);
    }

    
    public override bool CanRightClick(PlayerController caller, Vector3Int tilePosition)
    {
        if (caller.world.triggerTilemap.GetTile(tilePosition) == tile)
        {
            return false;
        }

        return true;
    }
}
