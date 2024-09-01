using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public bool offline;
    
    public override void Start()
    {
        if (offline)
        {
            OfflineMode();
        }
        else
        {
            base.Start();
        }
    }

    public void OfflineMode()
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(new NetworkConnectionToClient(1), player);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        // Serialize the current world state
        byte[] worldData = WorldGenerator.Instance.SerializeWorldData();

        // Send this data to the newly joined client
        WorldGenerator.Instance.SendWorldDataInChunks(conn, worldData);
    }
}
