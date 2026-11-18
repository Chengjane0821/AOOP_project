using UnityEngine;

namespace ForgottenVillage {
    public sealed class PlanetaryDefenceProjectile : MonoBehaviour {
        public PlanetaryDefenceRun run;
        public PlanetaryDefenceThreat Target { get; private set; }
        int targetId;
        public void Launch(PlanetaryDefenceThreat target, Vector3 origin) {
            Target = target; targetId = target.Id; transform.position = origin; gameObject.SetActive(true);
        }
        public void Step(float dt) {
            if (!gameObject.activeSelf) return;
            if (!Target || !Target.Live || Target.Id != targetId) { gameObject.SetActive(false); return; }
            Vector3 delta = Target.transform.position - transform.position;
            float step = 6 * dt;
            if (delta.magnitude <= step + .12f) {
                run.Resolve(Target, true); Target = null; gameObject.SetActive(false); return;
            }
            transform.position += delta.normalized * step;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90);
        }
    }
}
