using UnityEngine;
namespace ForgottenVillage {
    [CreateAssetMenu(menuName="Forgotten Village/Power-grid tuning")]
    public sealed class PowerGridTuning : ScriptableObject {
        public Color sourceA=new Color(.15f,.75f,1f),sourceB=new Color(1f,.65f,.2f);
        public Color offline=new Color(.12f,.2f,.26f),online=new Color(.25f,1f,.65f),fault=new Color(1f,.18f,.15f);
        [Range(0,.5f)]public float volume=.16f;
        [Range(.01f,.08f)]public float cableWidth=.025f;
        [Range(15,180)]public float machineSpeed=85;
    }
}
