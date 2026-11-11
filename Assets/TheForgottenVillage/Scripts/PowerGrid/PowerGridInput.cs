using UnityEngine;
using UnityEngine.InputSystem;
namespace ForgottenVillage {
    public sealed class PowerGridInput : MonoBehaviour {
        public PowerGridRun run;
#if UNITY_EDITOR
        public System.Action BeforeCapture;
#endif
        void Update(){
            if(!run||!run.AcceptsInput||run.house.game.player.IsVR)return;
#if UNITY_EDITOR
            BeforeCapture?.Invoke();
#endif
            var keys=Keyboard.current;
            if(keys!=null){
                if(keys.aKey.wasPressedThisFrame)run.SelectSource(0);
                if(keys.sKey.wasPressedThisFrame)run.SelectSource(1);
                var sockets=new[]{keys.digit1Key,keys.digit2Key,keys.digit3Key,keys.digit4Key,keys.digit5Key};
                for(int i=0;i<sockets.Length;i++)if(sockets[i].wasPressedThisFrame)run.ConnectMachine(i);
                if(keys.spaceKey.wasPressedThisFrame)run.TogglePower();
                if(keys.rKey.wasPressedThisFrame)run.ResetBreaker(run.SelectedSource);
                if(keys.bKey.wasPressedThisFrame)run.ToggleBackup();
            }
            var mouse=Mouse.current;
            if(mouse!=null&&mouse.leftButton.wasPressedThisFrame){
                var ray=run.house.game.player.View.ScreenPointToRay(mouse.position.ReadValue());
                if(Physics.Raycast(ray,out var hit,5f)){
                    var target=hit.collider.GetComponentInParent<PowerGridControl>();if(target)target.Use();
                }
            }
        }
    }
}
