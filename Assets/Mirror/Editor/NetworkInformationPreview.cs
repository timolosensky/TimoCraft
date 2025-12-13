using UnityEngine;
using UnityEditor;

namespace Mirror
{
    [CustomPreview(typeof(NetworkIdentity))]
    public class NetworkInformationPreview : ObjectPreview
    {
        class Styles
        {
            public GUIStyle label = new GUIStyle(EditorStyles.label);
            public GUIStyle boldLabel = new GUIStyle(EditorStyles.boldLabel);

            public Styles()
            {
                label.wordWrap = true;
                boldLabel.wordWrap = true;
            }
        }

        // Lazy Loading um NullReferenceException zu verhindern
        private Styles styles;
        Styles GetStyles()
        {
            if (styles == null) styles = new Styles();
            return styles;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Network Information");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (target == null)
                return;

            NetworkIdentity identity = target as NetworkIdentity;
            if (identity == null)
                return;

            Rect rect = r;
            rect.x += 5;
            rect.y += 5;
            rect.width -= 10;
            rect.height -= 10;

            GUILayout.BeginArea(rect);

            GUILayout.Label($"Asset ID: {identity.assetId}", GetStyles().label);
            GUILayout.Label($"Scene ID: {identity.sceneId:X}", GetStyles().label);
            GUILayout.Label($"Net ID: {identity.netId}", GetStyles().boldLabel);

            GUILayout.Space(10);

            if (identity.isServer)
                GUILayout.Label("Server: Active", GetStyles().boldLabel);
            else
                GUILayout.Label("Server: Inactive", GetStyles().label);

            if (identity.isClient)
                GUILayout.Label("Client: Active", GetStyles().boldLabel);
            else
                GUILayout.Label("Client: Inactive", GetStyles().label);

            if (identity.isLocalPlayer)
                GUILayout.Label("Local Player: Yes", GetStyles().boldLabel);
            else
                GUILayout.Label("Local Player: No", GetStyles().label);

            // FIX: Wir prüfen nur, ob die Verbindung da ist, statt auf connectionId zuzugreifen
            if (identity.connectionToClient != null)
                GUILayout.Label("Client Connection: Connected", GetStyles().label);

            if (identity.connectionToServer != null)
                GUILayout.Label("Server Connection: Connected", GetStyles().label);

            GUILayout.EndArea();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            styles = null;
        }
    }
}