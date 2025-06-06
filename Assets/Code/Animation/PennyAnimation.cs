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
	
	private bool WalkingAway = false;
	private bool Walking = false;
	
	private Routine _currentAnimRoutine;

	//[SerializeField]
	//List<Transform> _startLocations = new List<Transform>(5);

    void Awake()
    {

    }

	void OnDestroy()
	{
		if(_currentAnimRoutine != null) {
			_currentAnimRoutine.Stop();
		}
	}
	
	public IEnumerator Walk(Transform location, float duration)
	{
		if(!Walking)
		{
			Walking = true;
			
			if(_animator != null) {
				_animator.ResetTrigger("stand");
				//_animator.ResetTrigger("turnaround");
				_animator.SetBool("walking", true);
				yield return 0.5f;
				
				Quaternion startRot = transform.rotation;
				
				float tRot = 0f;
				while(tRot < (duration/4))
				{
					if(location != null && transform != null) {
						Quaternion newRot = Quaternion.Slerp(startRot, location.rotation, tRot/(duration/4));
						transform.rotation = newRot;
					}
					tRot += UnityEngine.Time.unscaledDeltaTime;
					yield return null;
				}
				
				float t = 0f;
				Vector3 startPos = transform.position;
				while(t < duration)
				{
					if(location != null && transform != null) {
						Vector3 newPos = Vector3.Lerp(startPos, location.position, t/duration);
						transform.position = newPos;
					}
					t += UnityEngine.Time.unscaledDeltaTime;
					yield return null;
				}
				
				
				if(_animator != null) {
					if(WalkingAway) {
						//_animator.SetTrigger("turnaround");		
					}
				}
				
				_animator.SetBool("walking", false);

				WalkingAway = !WalkingAway;
				
				yield return 1f;
			}
		}
		
		Walking = false;

		if(_animator != null) {
			int waitTime = (int)UnityEngine.Random.Range(2, 8);

			yield return waitTime;

			float noteBookOrBinoculars = UnityEngine.Random.Range(0.0f,1.0f);
			//Debug.Log(noteBookOrBinoculars);
			if(noteBookOrBinoculars < 0.33f) {
				_currentAnimRoutine.Replace(NotebookWrite());
			} else if(noteBookOrBinoculars >= 0.33f && noteBookOrBinoculars <= 0.66f) {
				_currentAnimRoutine.Replace(KneelAndTinker());
			} else {
				_currentAnimRoutine.Replace(LookBinoculars());
			}
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
		
		//Debug.Log("WALKING AWAY: " + WalkingAway);
		if(!WalkingAway) {
			_currentAnimRoutine.Replace(Walk(NearGate, 5f));
		} else {
			_currentAnimRoutine.Replace(Walk(StartPoint, 5f));
		}
	}
	
	public IEnumerator Wave()
	{
		yield return 1f;
		if(_animator != null) {
			_animator.SetBool("wave", true);
					
			float writeTime = UnityEngine.Random.Range(10.0f, 15.0f);
			yield return writeTime;
			_animator.SetBool("wave", false);
			_currentAnimRoutine.Replace(TurnAround());
		}
	}

	public IEnumerator NotebookWrite()
	{
		yield return 1f;
		if(_animator != null) {
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
	}

	public IEnumerator LookBinoculars()
	{
		yield return 1f;
		if(_animator != null) {
			_animator.SetBool("binoculars", true);
			if(Binoculars != null) {
				Binoculars.GetComponent<Animator>().SetTrigger("BinocularAnim_isPlaying");
			}
			
			float lookTime = UnityEngine.Random.Range(15.0f, 20.0f);
			yield return lookTime;
			_animator.SetBool("binoculars", false);
			_currentAnimRoutine.Replace(TurnAround());
		}
	}

	public IEnumerator KneelAndTinker()
	{
		yield return 1f;
		if(_animator != null) {
			_animator.SetBool("kneeling", true);
			_animator.SetBool("tinkering", true);
					
			float writeTime = UnityEngine.Random.Range(10.0f, 15.0f);
			yield return writeTime;

			_animator.SetTrigger("stand");
			_animator.SetBool("tinkering", false);
			_animator.SetBool("kneeling", false);
			yield return 3f;
			_currentAnimRoutine.Replace(TurnAround());
		}
	}

	public IEnumerator WalkToStart()
	{
		yield return null;
		_currentAnimRoutine.Replace(Walk(FarPoint, 5f));
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
		WalkingAway = false;
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
			_animator.SetBool("write", false);
			_animator.SetBool("walking", false);
			_animator.SetBool("kneeling", false);
			_animator.SetBool("tinkering", false);
			_animator.ResetTrigger("turnaround");
			_animator.ResetTrigger("stand");
			
			_animator.Play("Idle", 0);
		}	
	}

	[LeafMember("LoopToGate"), Preserve]
	public void LoopToGate()
	{
		if(_animator != null)
		{
			_currentAnimRoutine.Replace(Walk(NearGate, 5f));
		}
	}

	[LeafMember("Wave"), Preserve]
	public void Leaf_Wave()
	{
		if(_currentAnimRoutine != null) {
			_currentAnimRoutine.Replace(Wave());
		}
	}
}
