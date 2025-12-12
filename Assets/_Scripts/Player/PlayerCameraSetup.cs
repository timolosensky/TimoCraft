using UnityEngine;
using Mirror; // Wichtig für NetworkBehaviour

public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("Wo soll die Kamera hin?")]
    public Transform cameraMountPoint; // Hier ziehen wir gleich das "Augen"-Objekt rein

    // Diese Methode ruft Mirror automatisch auf, sobald DEIN Spieler auf DEINEM PC spawnt.
    public override void OnStartLocalPlayer()
{
    Debug.Log("--- START SETUP ---");

    // 1. Prüfen: Ist der Mount Point da?
    if (cameraMountPoint == null)
    {
        Debug.LogError("FEHLER: 'Camera Mount Point' ist NICHT zugewiesen! Bitte im Player Prefab reinziehen.");
        return;
    }
    else
    {
        Debug.Log("Info: Mount Point gefunden: " + cameraMountPoint.name);
    }

    // 2. Prüfen: Ist die Kamera da?
    Camera mainCam = Camera.main;
    if (mainCam == null)
    {
        Debug.LogError("FEHLER: Unity findet keine 'MainCamera'. Ist sie in der Szene? Ist sie AKTIV? Hat sie den TAG?");
        return;
    }
    else
    {
        Debug.Log("Info: Kamera gefunden: " + mainCam.name);
    }

    // 3. Durchführen
    mainCam.transform.SetParent(cameraMountPoint);
    mainCam.transform.localPosition = Vector3.zero; // WICHTIG: Auf 0 setzen
    mainCam.transform.localRotation = Quaternion.identity; // Rotation nullen

    mainCam.gameObject.SetActive(true);

    Debug.Log("ERFOLG: Kamera wurde an den Kopf geklebt und aktiviert!");
}

    // Wenn der Spieler stirbt oder disconnected, lassen wir die Kamera wieder los
    void OnDisable()
    {
        if (isLocalPlayer && Camera.main != null)
        {
            Camera.main.transform.SetParent(null);
        }
    }
}