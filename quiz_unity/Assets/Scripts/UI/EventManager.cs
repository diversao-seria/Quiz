using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
    private PressClock holdTouchClock;

    // Boolean that represents if an answerbutton was touched.
    private bool wasTouched;

    // Boolean that represents if it's possible to select an answerButton
    public bool idleState;

    private List<GameObject> answerButtonList;

    public float answerConfirmation = 3.0f;   

    private AnswerButton selectedAnswerButton;
    private Slider progressSlider;

    public void Awake()
    {
        wasTouched = false;
        idleState = true;
        holdTouchClock = new PressClock(0);
    }

    public void Update()
    {
        if(wasTouched && !idleState)
        {
            Touch touch = Input.GetTouch(0);
            holdTouchClock.IncreaseTime(Time.deltaTime);

            // TO DO: PEgar o progesss sldier da alternativa que vai alterar o valor de was touched
            progressSlider.value = Math.Min(holdTouchClock.Time / answerConfirmation, 1);

            if(holdTouchClock.Time >= answerConfirmation)
            {
                resetSlider();
                holdTouchClock.Reset();
                selectedAnswerButton.GetComponent<AnswerButton>().HandleClick();
            }
            else
            {
                if (Input.touchCount <= 0 || touch.phase == TouchPhase.Ended)
                {
                    idleState = true;
                    resetSlider();
                    holdTouchClock.Reset();
                }
            }
        }
    }

    // Called from AnswerButton object as pointdown event.
    public void buttonTouched(AnswerButton answerButton)
    {

        if(idleState == true)
        {
            selectedAnswerButton = answerButton;
            progressSlider = (Slider)answerButton.transform.Find("Slider").gameObject.GetComponent<Slider>();
            wasTouched = true;
            progressSlider.gameObject.SetActive(true);
            idleState = false;
        }
    }

    private void resetSlider()
    {
            wasTouched = false;
            progressSlider.value = 0;
            progressSlider.gameObject.SetActive(false);
    }

    public void questionDone()
    {
        idleState = true;
    }

    public bool TouchLastStatus()
    {
        return wasTouched;
    }

    public void SetAlternativesReference(List <GameObject> answerButtonList)
    {
        this.answerButtonList = answerButtonList;
    }
}
