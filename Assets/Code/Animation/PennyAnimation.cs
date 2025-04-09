using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

public class PennyAnimation : ScriptActorComponent 
{
	[SerializeField]
	Animator _animator;

	[SerializeField]
	GameObject Notebook;
	
	[SerializeField]
	GameObject Pen;
	
	[SerializeField]
	GameObject Binoculars;

	[SerializeField]
	Transform StartPoint;

	[SerializeField]
	Transform NearGate;
	
	[SerializeField]
	Transform FarPoint;
	
	private bool ToggleSpot = false;
	private bool Walking = false;

	private bool TurnedAround = true;
	private bool TurningAround = true;
	
	private Routine _currentAnimRoutine;

	//[SerializeField]
	//List<Transform> _startLocations = new List<Transform>(5);

    void Awake()
    {

    }
	
	public IEnumerator Walk(Transform location, float duration)
	{
		if(!Walking)
		{
			Walking = true;
			
			_animator.SetBool("walking", true);
			yield return 0.5f;
			
			float t = 0f;
			Vector3 startPos = transform.position;
			Quaternion startRot = transform.rotation;
			while(t < duration)
			{
				Vector3 newPos = Vector3.Lerp(startPos, location.position, t/duration);
				Quaternion newRot = Quaternion.Slerp(startRot, location.rotation, t/duration);
				transform.position = newPos;
				transform.rotation = newRot;
				t += UnityEngine.Time.unscaledDeltaTime;
				yield return null;
			}
			
			ToggleSpot = !ToggleSpot;
			_animator.SetBool("walking", false);
			
			yield return 2f;
		}
		
		Walking = false;
		TurnedAround = false;
	}
	
	public IEnumerator TurnAround(bool doPoint=true)
	{
		if(!TurnedAround)
		{
			TurningAround = true;
			
			//do a point here...
			//if(doPoint)
			//{
			//	_animator.SetTrigger("point00");
			//}
			
			yield return new WaitForSeconds(3f);
			
			_animator.SetTrigger("turnaround");
			
			yield return new WaitForSeconds(3f);
			
		}
		
		TurnedAround = true;
		
	}
	
	public IEnumerator NotebookWrite()
	{
		yield return null;
		_animator.SetBool("write", true);
		int writeTime = UnityEngine.Random.Range(5, 15);
		yield return writeTime;

	}

	public IEnumerator LookBinoculars()
	{
		yield return null;
		_animator.SetBool("binoculars", true);
		int lookTime = UnityEngine.Random.Range(5, 15);
		yield return lookTime;
		
	}

	public IEnumerator WalkToGate()
	{

		/*_animator.SetBool("kneeling", true);
		_animator.SetBool("tinkering", true);
		
		yield return new WaitForSeconds(1f);
		
		_animator.SetTrigger("stand");*/
		
		yield return null;
		_currentAnimRoutine.Replace(Walk(NearGate, 3f));
	}

	public IEnumerator WalkToStart()
	{
		yield return null;
		_currentAnimRoutine.Replace(Walk(FarPoint, 3f));
	}
	
	/*public void SetStartingLocation(int index)
	{
		if(index < _startLocations.Count)
		{
			transform.position = _startLocations[index].position;
			transform.rotation = _startLocations[index].rotation;
		}
	}*/
	
	public void StopAllAnimations()
	{
		ToggleSpot = false;
		Walking = false;
		TurnedAround = true;
		TurningAround = false;
		
		if(Notebook != null)
		{
			Notebook.SetActive(false);
		}
		
		if(Pen != null)
		{
			Pen.SetActive(false);
		}
		
		if(_animator != null)
		{
			_animator.SetBool("writing", false);
			_animator.SetBool("walking", false);
			_animator.SetBool("tinkering", false);
			_animator.SetBool("standtinker", false);
			_animator.SetBool("kneeling", false);
			_animator.ResetTrigger("turnaround");
			_animator.ResetTrigger("stand");
			
			_animator.Play("Idle", 0);
		}	
	}
	
	public void StartWriting()
	{
		if(_animator != null)
		{
			_animator.SetBool("writing", true);
		}
	}
	
	public void StartTinkering()
	{
		if(_animator != null)
		{
			_animator.SetBool("standtinker", true);
		}
	}
	
	[LeafMember("LoopToGate"), Preserve]
	public void LoopToGate()
	{
		if(_animator != null)
		{
			_currentAnimRoutine.Replace(WalkToGate());
		}
	}
	
	public void StartKneeling()
	{
		if(_animator != null)
		{
			_animator.SetBool("kneeling", true);
			_animator.SetBool("tinkering", true);
		}
	}
}
