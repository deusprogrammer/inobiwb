using UnityEngine;

[CreateAssetMenu(fileName = "ClutterBlockIdleState", menuName = "States/ClutterBlock Idle State")]
public class ClutterBlockIdleState : ClutterBlockState
{
    public override void OnEvent(string eventName, GameObjectStateController controller)
    {
        ClutterBlockStateController blockController = (ClutterBlockStateController)controller;
        Debug.Log($"[Block at {blockController.transform.position}] ClutterBlockIdleState.OnEvent: {eventName}");
        if (eventName == "push")
        {
            Debug.Log($"[Block at {blockController.transform.position}] Changing to PUSHED state");
            controller.ChangeState(ClutterBlockStates.PUSHED);
        }
    }
}
