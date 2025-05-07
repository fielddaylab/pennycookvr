using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OGD;

namespace Pennycook {
    public class LoadSurvey : MonoBehaviour
    {
        [SerializeField]
        TextAsset SurveyAsset;

        [SerializeField]
        SurveyLikert SurveyLikert1;

        [SerializeField]
        SurveyLikert SurveyLikert2;

        [SerializeField]
        SurveyNextButton    NextButton;

        SurveyPackage OpenGameDataSurvey;
        
        int CurrentSurvey = 0;
        int CurrentPage = 0;

        static string[] DefaultResponse = new string[7] {"Strongly Disagree", "Disagree", "Somewhat Disagree", "Neutral", "Somewhat Agree", "Agree", "Strongly Agree"};

        void Awake()
        {
            if(SurveyAsset != null) {
                OpenGameDataSurvey = SurveyPackage.Parse(SurveyAsset.text);
                LoadLikertQuestions(true);
            }

            NextButton.OnPressed.Register(NextQuestions);
        }

        public void LoadLikertQuestions(bool isCustom=false)
        {
            SurveyLikert1.IsCustom = isCustom;
            SurveyLikert2.IsCustom = isCustom;

            if(isCustom) {
                SurveyLikert1.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[0].Prompt;
                SurveyLikert2.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[1].Prompt;
                //load in custom responses...
                for(int i = 0; i < OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[0].Responses.Length; ++i) {
                    SurveyLikert1.SetCustomResponse(OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[0].Responses[i], i);
                }

                for(int i = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[0].Responses.Length; i < 7; ++i) {
                    SurveyLikert1.SetEnabled(false, i);
                }

                for(int i = 0; i < OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[1].Responses.Length; ++i) {
                    SurveyLikert2.SetCustomResponse(OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[1].Responses[i], i);
                }

                for(int i = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[1].Responses.Length; i < 7; ++i) {
                    SurveyLikert2.SetEnabled(false, i);
                }
            } else {
                SurveyLikert1.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[0].Prompt;
                SurveyLikert2.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[1].Prompt;

                for(int i = 0; i < DefaultResponse.Length; ++i) {
                    SurveyLikert1.SetCustomResponse(DefaultResponse[i], i);
                    SurveyLikert1.SetEnabled(true, i);
                }

                for(int i = 0; i < DefaultResponse.Length; ++i) {
                    SurveyLikert2.SetCustomResponse(DefaultResponse[i], i);
                    SurveyLikert2.SetEnabled(true, i);
                }
            }
        }

        public void NextQuestions()
        {
            if(CurrentPage == OpenGameDataSurvey.Surveys[CurrentSurvey].Pages.Length-1) {
                CurrentSurvey++;
                CurrentPage = 0;
            } else {
                CurrentPage++;
            }

            LoadLikertQuestions(CurrentSurvey == 0);
        }
    }
}
