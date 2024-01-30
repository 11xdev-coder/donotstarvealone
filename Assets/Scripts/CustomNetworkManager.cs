using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        // Serialize the current world state
        byte[] worldData = WorldGenerator.Instance.SerializeWorldData();

        // Send this data to the newly joined client
        WorldGenerator.Instance.SendWorldDataInChunks(conn, worldData);
    }
}
