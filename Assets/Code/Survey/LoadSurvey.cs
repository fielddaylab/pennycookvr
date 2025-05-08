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
        int CurrentQuestion = 0;

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
                SurveyLikert1.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion].Prompt;
                SurveyLikert2.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion+1].Prompt;
                //load in custom responses...
                for(int i = 0; i < OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion].Responses.Length; ++i) {
                    SurveyLikert1.SetCustomResponse(OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion].Responses[i], i);
                }

                for(int i = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion].Responses.Length; i < 7; ++i) {
                    SurveyLikert1.SetEnabled(false, i);
                }

                for(int i = 0; i < OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion+1].Responses.Length; ++i) {
                    SurveyLikert2.SetCustomResponse(OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion+1].Responses[i], i);
                }

                for(int i = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion+1].Responses.Length; i < 7; ++i) {
                    SurveyLikert2.SetEnabled(false, i);
                }
            } else {
                SurveyLikert1.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion].Prompt;
                for(int i = 0; i < DefaultResponse.Length; ++i) {
                    SurveyLikert1.SetCustomResponse(DefaultResponse[i], i);
                    SurveyLikert1.SetEnabled(true, i);
                }

                if(CurrentQuestion+1 < OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions.Length) {
                    SurveyLikert2.gameObject.SetActive(true);
                    SurveyLikert2.GetQuestion().text = OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions[CurrentQuestion+1].Prompt;
                    for(int i = 0; i < DefaultResponse.Length; ++i) {
                        SurveyLikert2.SetCustomResponse(DefaultResponse[i], i);
                        SurveyLikert2.SetEnabled(true, i);
                    }
                } else {
                    SurveyLikert2.gameObject.SetActive(false);
                }

            }
        }
		/*void Update()
		{
			if(UnityEngine.Input.GetKeyDown("q")) {
				NextQuestions();
			}
		}*/

        public void NextQuestions()
        {
            CurrentQuestion+=2;
            if(CurrentQuestion >= OpenGameDataSurvey.Surveys[CurrentSurvey].Pages[CurrentPage].Questions.Length) {
                CurrentPage++;
                CurrentQuestion = 0;
                if(CurrentPage >= OpenGameDataSurvey.Surveys[CurrentSurvey].Pages.Length) {
                    CurrentSurvey++;
                    CurrentPage = 0;
                    CurrentQuestion = 0;
                }
            } 

            //Debug.Log(CurrentSurvey + " " + CurrentPage + " " + CurrentQuestion);

            if(CurrentSurvey < OpenGameDataSurvey.Surveys.Length) {
                LoadLikertQuestions(CurrentSurvey == 0);
            } else {
                //send in the data...and hide survey.
                gameObject.SetActive(false);
            }
        }
    }
}
