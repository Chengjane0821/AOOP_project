using UnityEngine;

namespace ForgottenVillage {
    public sealed class BridgeSocket : MonoBehaviour {
        public BridgeRun run;
        public int index;
        public bool support;
        public float requiredLength = .8f;
        public Transform snapPoint;
        public BridgePiece Occupant { get; private set; }
        public Transform Point => snapPoint ? snapPoint : transform;

        public bool CanAccept(BridgePiece piece, Vector3 position, float localYaw) {
            if (!isActiveAndEnabled || !piece || piece.support != support ||
                (Occupant && Occupant != piece) || !BridgeRules.Aligned(localYaw)) return false;
            Vector3 d = run ? run.transform.InverseTransformVector(position - Point.position) : position - Point.position;
            return new Vector2(d.x, d.z).magnitude <= BridgeRules.SnapDistance && Mathf.Abs(d.y) <= (support ? .48f : .34f);
        }

        internal void Attach(BridgePiece piece) {
            if (Occupant && Occupant != piece) return;
            Occupant = piece;
            piece.Attach(this);
        }

        internal void Detach(BridgePiece piece) {
            if (Occupant == piece) Occupant = null;
        }

        internal void Clear() {
            Occupant = null;
        }
    }
}
