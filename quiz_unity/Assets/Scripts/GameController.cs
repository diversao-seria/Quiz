using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;

public class GameController : MonoBehaviour
{
	public SimpleObjectPool answerButtonObjectPool;
	public Text questionText;
	public Text scoreDisplay;
	public Text timeRemainingDisplay;
	public Transform answerButtonParent;
	public GameObject questionDisplay;
	public GameObject roundEndDisplay;
	public Text highScoreDisplay;
	public Image feedbackImage;
	public Sprite correctAnswerIcon;
	public Sprite wrongAnswerIcon;

	private DataController dataController;
	private RoundData currentRoundData;

	private List<Question> questionPool;  // Question are going here.

	private bool isRoundActive = false;
	private bool isQuestionAnswered = false;

	private int playerScore;
	private int questionIndex;
	private List<GameObject> answerButtonGameObjects = new List<GameObject>();

	private Clock questionClock;
	private Clock quizClock;

	private Question[] questions;

	void Start()
	{
		dataController = FindObjectOfType<DataController>();                                // Store a reference to the DataController so we can request the data we need for this round

		currentRoundData = dataController.GetCurrentRoundData();                            // Ask the DataController for the data for the current round. At the moment, we only have one round - but we could extend this
		questionPool = dataController.RetrieveQuiz().GetQuestionData().Questions;      // Take a copy of the questions so we could shuffle the pool or drop questions from it without affecting the original RoundData object

		questionClock = new Clock(30);
		quizClock = new Clock(0);

		UpdateTimeRemainingDisplay(quizClock);
		playerScore = 0;
		questionIndex = 0;

		ShowQuestion();
		isRoundActive = true;
	}

	void Update()
	{
		// what is round active?
		if (isRoundActive && !isQuestionAnswered)
		{
			quizClock.IncreaseTime(Time.deltaTime);
			questionClock.DecreaseTime(Time.deltaTime);
			UpdateTimeRemainingDisplay(questionClock);

			if (questionClock.Time <= 0f)                                                        // If timeRemaining is 0 or less, the round ends
			{
				if(questionIndex == questionPool.Count - 1)
                {
					EndRound();
				}
				else
				{
					// Runs out of time to answer -> need to save this information (future commits)
					AnswerButtonClicked(false);
				}
			}
		}
	}

	void ShowQuestion()
	{
		RemoveAnswerButtons();

		Question question = questionPool[questionIndex];                                     // Get the QuestionData for the current question																								 // Update questionText with the correct tex
		questionText.text = question.Text;

		for (int i = 0; i < question.Alternatives.Count; i++)                               // For every AnswerData in the current QuestionData...
		{
			GameObject answerButtonGameObject = answerButtonObjectPool.GetObject();         // Spawn an AnswerButton from the object pool
			answerButtonGameObjects.Add(answerButtonGameObject);
			answerButtonGameObject.transform.SetParent(answerButtonParent);
			answerButtonGameObject.transform.localScale = Vector3.one;

			AnswerButton answerButton = answerButtonGameObject.GetComponent<AnswerButton>();

			answerButton.SetUp(question.Alternatives[i]);                           // Pass the AnswerData to the AnswerButton so the AnswerButton knows what text to display and whether it is the correct answer
		}
	}

	void RemoveAnswerButtons()
	{
		while (answerButtonGameObjects.Count > 0)                                           // Return all spawned AnswerButtons to the object pool
		{
			answerButtonObjectPool.ReturnObject(answerButtonGameObjects[0]);
			answerButtonGameObjects.RemoveAt(0);
		}
	}

	public void AnswerButtonClicked(bool isCorrect)
	{
		isQuestionAnswered = true;
		if (isCorrect)
		{
				playerScore += currentRoundData.pointsAddedForCorrectAnswer;                    // If the AnswerButton that was clicked was the correct answer, add points
				scoreDisplay.text = playerScore.ToString();
		}


		StartCoroutine(VisualFeedback(isCorrect));
	}

	private void UpdateTimeRemainingDisplay(Clock clock)
	{
		timeRemainingDisplay.text = Mathf.Round(clock.Time).ToString();
	}

	public void EndRound()
	{
		isRoundActive = false;

		dataController.SubmitNewScore(playerScore);
		highScoreDisplay.text = dataController.GetHighestPlayerScore().ToString();

		questionDisplay.SetActive(false);
		roundEndDisplay.SetActive(true);

		Debug.Log("Total time: " + quizClock.HHmmss());
	}

	public void EndQuestion()
    {

    }

	public void ReturnToMenu()
	{
		SceneManager.LoadScene("MenuScreen");
	}

	IEnumerator VisualFeedback(bool isCorrect)
	{
        if(isCorrect) 
		{
			feedbackImage.GetComponent<Image>().sprite = correctAnswerIcon;
		}
		else
        {
			feedbackImage.GetComponent<Image>().sprite = wrongAnswerIcon;
		}

		feedbackImage.gameObject.SetActive(true);
		yield return new WaitForSeconds(3);
		feedbackImage.gameObject.SetActive(false);
		isQuestionAnswered = false;

		if (questionPool.Count > questionIndex + 1)                                         // If there are more questions, show the next question
		{
			questionIndex++;
			questionClock.NewCountdown(30);
			ShowQuestion();
		}
		else                                                                                // If there are no more questions, the round ends
		{
			EndRound();
		}

	}
}