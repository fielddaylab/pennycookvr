using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustHeight : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    /*override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 pos = animator.gameObject.transform.GetChild(0).position;
        pos.y -= 1f;
        animator.gameObject.transform.GetChild(0).position = pos;
    }*/

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    /*override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 pos = animator.transform.position;
        pos.y -= 1f;
        animator.transform.position = pos;
    }*/

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    /*override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Vector3 pos = animator.gameObject.transform.GetChild(0).position;
        pos.y -= 1f;
        animator.gameObject.transform.GetChild(0).position = pos;
    }*/

    // OnStateMove is called right after Animator.OnAnimatorMove()
   // override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
        // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
