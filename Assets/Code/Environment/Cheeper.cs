using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Cheeper : MonoBehaviour
{
    public enum CheepState {
        None, // no cheeping
        Muffled, // muffled cheeping
        Open // unmuffled cheeping
    }

    [SerializeField] private AudioClip _muffledCheep, _normalCheep; // quality of cheep
    [SerializeField] private float _cheepRate = 0; // rate at which cheeps occur (in cheeps per minute)
    [SerializeField] private float _volMod = 0.75f;
    
    [SerializeField] private float _overallCheepTime = 10f;

    private float _cheepTimer = 0;
    private float _cheepTime;

    private float _totalTime = 0;

    private CheepState _state;

    private AudioSource m_audioSrc;

    private Routine _fadeRoutine;

    private void Awake() {
        m_audioSrc = GetComponent<AudioSource>();
    }

    public void SetState(CheepState state) {
        _state = state;

        /*
        switch (_state) {
            case CheepState.None:
                m_audioSrc.clip = null;
                break;
            case CheepState.Muffled:
                m_audioSrc.clip = _muffledCheep;
                break;
            case CheepState.Open:
                m_audioSrc.clip = _normalCheep;
                break;
            default:
                break;
        }
        */
    }

    public void SetFade(float min, float max, float duration) {
        m_audioSrc.volume = min * _volMod;
        _fadeRoutine.Replace(FadeRoutine(min, max, duration));
    }

    public void SetRate(float rate) {
        _cheepRate = rate;
        _cheepTimer = 0;
        _cheepTime = (60f / _cheepRate);
    }

    private IEnumerator FadeRoutine(float min, float max, float duration) {
        yield return Tween.Float(min, max, (v) => { m_audioSrc.volume = v * _volMod; }, duration);
    }

    private void Update() {
        if (_state != CheepState.None && _cheepRate > 0 && _cheepTimer >= _cheepTime && _totalTime < _overallCheepTime) {
            AudioClip clip;
            if (_state == CheepState.Muffled) {
                clip = _muffledCheep;
            }
            else {
                clip = _normalCheep;
            }

            m_audioSrc.pitch = Random.Range(0.95f, 1.05f);
            m_audioSrc.PlayOneShot(clip);
            float variance = 0.5f;
            _cheepTimer = Random.Range(0, _cheepTime * variance);
            
            _totalTime += Time.deltaTime;
            if(_totalTime > _overallCheepTime) {
                _state = CheepState.None;
            }
        }
        _cheepTimer += Time.deltaTime;

    }
}
