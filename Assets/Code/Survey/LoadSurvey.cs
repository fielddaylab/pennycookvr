using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OGD;

public class LoadSurvey : MonoBehaviour
{
    [SerializeField]
    TextAsset SurveyAsset;

    [SerializeField]
    SurveyQuestion SurveyQuestion1;

    [SerializeField]
    SurveyQuestion SurveyQuestion2;

    SurveyPackage OpenGameDataSurvey;
    
    void Awake()
    {
        if(SurveyAsset != null) {
            OpenGameDataSurvey = SurveyPackage.Parse(SurveyAsset.text);
            for(int i = 0; i < OpenGameDataSurvey.Surveys.Length; ++i) {
                //Debug.Log(OpenGameDataSurvey.Surveys[i].Header);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
