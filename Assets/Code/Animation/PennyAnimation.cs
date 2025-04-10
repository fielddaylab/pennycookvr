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
		
		//Walking = false;

		int waitTime = (int)UnityEngine.Random.Range(2, 8);

		yield return waitTime;

		float noteBookOrBinoculars = UnityEngine.Random.Range(0.0f,1.0f);
		//Debug.Log(noteBookOrBinoculars);
		if(noteBookOrBinoculars < 0.33f) {
			_currentAnimRoutine.Replace(NotebookWrite());
		} else if(noteBookOrBinoculars >= 0.33f && noteBookOrBinoculars <= 0.66f) {
			_currentAnimRoutine.Replace(Wave());
		} else {
			_currentAnimRoutine.Replace(LookBinoculars());
		}
	}
	
	public IEnumerator TurnAround(bool doPoint=true)
	{
		//do a point here...
		//if(doPoint)
		//{
		//	_animator.SetTrigger("point00");
		//}
		
		yield return 0.5f;
		
		//_animator.SetTrigger("turnaround");
		//_animator.SetBool("walking", true);
		
		if(ToggleSpot) {
			_currentAnimRoutine.Replace(Walk(StartPoint, 3f));
		} else {
			_currentAnimRoutine.Replace(Walk(NearGate, 3f));
		}
	}
	
	public IEnumerator Wave()
	{
		_animator.SetBool("wave", true);
				
		float writeTime = UnityEngine.Random.Range(15.0f, 20.0f);
		yield return writeTime;
		_animator.SetBool("wave", false);
		_currentAnimRoutine.Replace(TurnAround());
	}

	public IEnumerator NotebookWrite()
	{
		yield return null;
		_animator.SetBool("write", true);

		if(Notebook != null && Pen != null) {
			Notebook.GetComponent<Animator>().SetTrigger("NotebookWrite_isPlaying");
			Pen.GetComponent<Animator>().SetTrigger("PenWrite_isPlaying");
		}
		
		float writeTime = UnityEngine.Random.Range(15.0f, 20.0f);
		yield return writeTime;
		_animator.SetBool("write", false);
		_currentAnimRoutine.Replace(TurnAround());
	}

	public IEnumerator LookBinoculars()
	{
		yield return null;
		_animator.SetBool("binoculars", true);
		if(Binoculars != null) {
			Binoculars.GetComponent<Animator>().SetTrigger("BinocularAnim_isPlaying");
		}
		
		float lookTime = UnityEngine.Random.Range(15.0f, 20.0f);
		yield return lookTime;
		_animator.SetBool("binoculars", false);
		_currentAnimRoutine.Replace(TurnAround());
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
			_currentAnimRoutine.Replace(Walk(NearGate, 3f));
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
