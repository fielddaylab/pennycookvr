using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OGD;
using FieldDay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        TMPro.TextMeshPro PlayerCode;

        SurveyPackage OpenGameDataSurvey;
        
        int CurrentSurvey = 0;
        int CurrentPage = 0;
        int CurrentQuestion = 0;

        int SurveyStopIndex = 1;

        private StringBuilder m_CachedBuilder = new StringBuilder(256);
        private List<SurveyQuestionResponse> m_AccumulatedResponses = new List<SurveyQuestionResponse>(8);

        static string[] DefaultResponse = new string[7] {"Strongly Disagree", "Disagree", "Somewhat Disagree", "Neutral", "Somewhat Agree", "Agree", "Strongly Agree"};

        void Awake()
        {
            NextButton.OnPressed.Register(NextQuestions);
        }

        void Start()
        {
            m_AccumulatedResponses.Clear();

            if (SurveyAsset != null)
            {
                OpenGameDataSurvey = SurveyPackage.Parse(SurveyAsset.text);
                if (PlayerCode != null)
                {
                    Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
                    if (pa != null) {
                        PlayerCode.text = "Player Code: " + pa.GetSessionID().ToString();
                    } 
                }
            }
        }

        public void SetSurveyIndex(int surveyIndex, int stopIndex) {
            CurrentSurvey = surveyIndex;
            SurveyStopIndex = stopIndex;
            CurrentQuestion = 0;
            CurrentPage = 0;
        }

        public void LoadLikertQuestions(bool isCustom=false)
        {
            gameObject.SetActive(true);

            SurveyLikert1.DeselectButtons();
            SurveyLikert2.DeselectButtons();
            
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

        public void TryAddResponse(string prompt, string response) {
            int fIndex = -1;
            for(int i = 0; i < m_AccumulatedResponses.Count; ++i) {
                if(m_AccumulatedResponses[i].Prompt == prompt) {
                    fIndex = i;
                }
            }

            if(fIndex == -1) {
                SurveyQuestionResponse r;
                r.Prompt = prompt;
                r.Response = response;
                r.Flag = "";
                m_AccumulatedResponses.Add(r);
            } else {
                SurveyQuestionResponse r = m_AccumulatedResponses[fIndex];
                r.Response = response;
                m_AccumulatedResponses[fIndex] = r;
            }

        }

        public void ActivateNext()
        {
            if(SurveyLikert1.IsPressed()) {
                if(SurveyLikert2.gameObject.activeSelf) {
                    NextButton.EnableNextButton(SurveyLikert2.IsPressed());
                    return;
                }
                NextButton.EnableNextButton(true);
                return;
            }

            NextButton.EnableNextButton(false);
        }

        public void NextQuestions()
        {
            int surveyIndex = CurrentSurvey;

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
            if(CurrentSurvey < OpenGameDataSurvey.Surveys.Length && CurrentSurvey != SurveyStopIndex) {
                LoadLikertQuestions(CurrentSurvey == 0);
            } else {
                //send in the data...and hide survey.
                SendData(surveyIndex);
                gameObject.SetActive(false);
            }
        }

        void SendData(int surveyIndex) {
            
            Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
            if (pa != null) {
                if (m_AccumulatedResponses.Count <= 0) {
                    return;
                }

                m_CachedBuilder.Clear().Append("{\"package_config_id\":\"");

                m_CachedBuilder = EscapeJS(m_CachedBuilder, OpenGameDataSurvey.PackageConfigId);
                m_CachedBuilder.Append("\",").Append("\"display_event_id\":\"");
                m_CachedBuilder = EscapeJS(m_CachedBuilder, OpenGameDataSurvey.Surveys[surveyIndex].DisplayEventId);
                m_CachedBuilder.Append("\",").Append("\"responses\":[");

                for(int i = 0; i < m_AccumulatedResponses.Count; i++) {
                    SurveyQuestionResponse response = m_AccumulatedResponses[i];
                    m_CachedBuilder.Append("{\"prompt\":\"");
                    m_CachedBuilder = EscapeJS(m_CachedBuilder, response.Prompt);
                    m_CachedBuilder.Append("\",").Append("\"response\":\"");
                    m_CachedBuilder = EscapeJS(m_CachedBuilder, response.Response);
                    m_CachedBuilder.Append("\"").Append("},");
                }

                TrimEnd(m_CachedBuilder, ',');
                m_CachedBuilder.Append("]}");

                //Debug.Log(m_CachedBuilder);
                pa.LogSurvey(m_CachedBuilder);

                m_CachedBuilder.Clear();
                m_AccumulatedResponses.Clear();
            }
        }

        void TrimEnd(StringBuilder builder, char endChar) {
            int length = builder.Length;
            while(length > 0 && builder[length - 1] == endChar) {
                length--;
            }
            builder.Length = length;
        }

        StringBuilder EscapeJS(StringBuilder builder, string text) {
            if (text == null || text.Length == 0) {
                return builder;
            }

            unsafe {
                fixed(char* textPin = text) {
                    char* ptr = textPin;
                    char* end = textPin + text.Length;
                    char c;
                    while(ptr != end) {
                        switch((c = *ptr++)) {
                            case '\\': {
                                builder.Append("\\\\");
                                break;
                            }
                            case '\"': {
                                builder.Append("\\\"");
                                break;
                            }
                            case '\n': {
                                builder.Append("\\n");
                                break;
                            }
                            case '\r': {
                                builder.Append("\\r");
                                break;
                            }
                            case '\t': {
                                builder.Append("\\t");
                                break;
                            }
                            case '\b': {
                                builder.Append("\\b");
                                break;
                            }
                            case '\f': {
                                builder.Append("\\f");
                                break;
                            }
                            default: {
                                builder.Append(c);
                                break;
                            }
                        }
                    }
                }
            }

            return builder;
        }
    }
}
