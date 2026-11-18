using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage {
    [RequireComponent(typeof(SphereCollider), typeof(XRSimpleInteractable))]
    public sealed class PlanetaryDefenceThreat : MonoBehaviour {
        public PlanetaryDefenceRun run;
        public GameObject[] variants;
        public int Id { get; private set; }
        public bool Targeted { get; set; }
        public bool Live => gameObject.activeSelf;
        float speed;
        void Awake() {
            GetComponent<XRSimpleInteractable>().selectEntered.AddListener(args => {
                if (run) run.Fire(this, args.interactorObject.transform.position);
            });
        }
        public void Launch(int id, Vector3 position, float velocity) {
            Id = id; speed = velocity; Targeted = false;
            transform.localPosition = position;
            for (int i = 0; i < variants.Length; i++) variants[i].SetActive(i == id % variants.Length);
            gameObject.SetActive(true);
        }
        public void Step(float dt) {
            if (!Live) return;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, run.planet.localPosition, speed * dt);
            Vector3 delta = run.planet.localPosition - transform.localPosition;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90);
            if (delta.magnitude <= run.planetRadius) run.Resolve(this, false);
        }
    }
}
