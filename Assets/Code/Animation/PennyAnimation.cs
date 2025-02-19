using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PennyAnimation : MonoBehaviour
{
	[SerializeField]
	Animator _animator;
	
	public GameObject Notebook;
	
	public GameObject Pen;
	
	public GameObject StartPointNW;
	public GameObject WalkPointNW;
	
	public GameObject StartPointS;	
	public GameObject WalkPointS;
	public GameObject WalkPointS2;
	
	[SerializeField]
	List<Transform> _startLocations = new List<Transform>(5);
	
	private bool ToggleSpot = false;
	private bool Walking = false;
	private bool WalkingBackAndForthS = false;
	private bool WalkingBackAndForthNW = false;
	private bool TurnedAround = true;
	private bool TurningAround = true;
	
    // Start is called before the first frame update
    void Start()
    {
        StartWalkLoopNW();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
	public IEnumerator Walk(Transform location, float duration)
	{
		if(!Walking)
		{
			Walking = true;
			
			_animator.SetBool("walking", true);
			yield return new WaitForSeconds(1f);
			
			float t = 0f;
			Vector3 startPos = transform.position;
			Quaternion startRot = transform.rotation;
			while(t < duration && (WalkingBackAndForthS || WalkingBackAndForthNW))
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
			
			yield return new WaitForSeconds(2f);
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
			if(doPoint)
			{
				_animator.SetTrigger("point00");
			}
			
			yield return new WaitForSeconds(3f);
			
			_animator.SetTrigger("turnaround");
			
			yield return new WaitForSeconds(3f);
			
		}
		
		TurnedAround = true;
		
	}
	
	public IEnumerator WalkBackAndForthS()
	{
		while(WalkingBackAndForthS)
		{
			if(!Walking)
			{
				if(!TurningAround)
				{
					StartCoroutine(TurnAround());
				}	
				
				if(TurnedAround)
				{
					TurningAround = false;
					
					if(ToggleSpot)
					{
						StartCoroutine(Walk(WalkPointS2.transform, 30f));
					}
					else
					{
						StartCoroutine(Walk(WalkPointS.transform, 30f));
					}
				}
			}

			yield return null;
		}
	}
	
	public IEnumerator WalkBackAndForthNW()
	{
		while(WalkingBackAndForthNW)
		{
			_animator.SetBool("kneeling", true);
			_animator.SetBool("tinkering", true);
			
			yield return new WaitForSeconds(1f);
			
			_animator.SetTrigger("stand");
			
			/*_animator.SetBool("kneeling", false);
			_animator.SetBool("tinkering", false);
			
			TurnedAround = false;
			
			StartCoroutine(TurnAround(false));
			
			StartCoroutine(Walk(WalkPointNW.transform, 3f));
			
			TurnedAround = false;
			
			StartCoroutine(TurnAround(false));
			
			StartCoroutine(Walk(StartPointNW.transform, 3f));*/
			
			//_animator.ResetTrigger("stand");
		}
	}
	
	public void SetStartingLocation(int index)
	{
		if(index < _startLocations.Count)
		{
			transform.position = _startLocations[index].position;
			transform.rotation = _startLocations[index].rotation;
		}
	}
	
	public void StopAllAnimations()
	{
		WalkingBackAndForthS = false;
		WalkingBackAndForthNW = false;
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
	
	public void StartWalkLoopNW()
	{
		if(_animator != null)
		{
			WalkingBackAndForthNW = true;
			StartCoroutine(WalkBackAndForthNW());
		}
	}
	
	public void StartWalkLoopS()
	{
		if(_animator != null)
		{
			WalkingBackAndForthS = true;
			StartCoroutine(WalkBackAndForthS());
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
