using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using System.IO;


public class GameController : MonoBehaviour
{
	public SimpleObjectPool answerButtonObjectPool;
	public Text questionText;
	public Text scoreDisplay;
	public Text timeRemainingDisplay;
	public Text questionNumberTextController;
	public Transform answerButtonParent;
	public GameObject questionDisplay;
	public GameObject roundEndDisplay;
	public Text highScoreDisplay;
	public Image feedbackImage;
	public Sprite correctAnswerIcon;
	public Sprite wrongAnswerIcon;
	public AudioClip[] audioClips;

	private DataController dataController;
	private RoundData currentRoundData;
	private JsonController jsonController;
	private PowerUpController powerUpController;
	private AudioSource audioSource;
	private EventManager eventManager;

	private List<Question> questionPool;  // Question are going here.

	private bool isRoundActive = false;
	private bool isQuestionAnswered = false;

	private int disabledAlternative = 0;
	private List<string> sequencia_atuacao = new List<string>();
	private string source = "Q-0-1-H-0-0-0-AE-0-T-0-S-0";
	private int h1 = 0, h2 = 0, h3 = 0;
	private int rightAnswers;
	private int playerScore;
	private int questionIndex;
	private List<GameObject> answerButtonGameObjects = new List<GameObject>();
	private int streak = 0;
	private GameObject[] answers;

	private QuestionClock questionClock;
	private QuizClock quizClock;

	enum Clip : int
    {
		correct,
		wrong,
		// power ups
    }

	void Start()
	{
		jsonController = FindObjectOfType<JsonController>();

		dataController = FindObjectOfType<DataController>();        // Store a reference to the DataController so we can request the data we need for this round

		audioSource = gameObject.GetComponent<AudioSource>();
		eventManager = GetComponent<EventManager>();


		// powerUpController = FindObjectOfType<PowerUpController>();

		currentRoundData = dataController.CurrentRoundData;                      // Ask the DataController for the data for the current round. At the moment, we only have one round - but we could extend this
		questionPool = dataController.RetrieveQuiz().GetQuestionData().Questions;      // Take a copy of the questions so we could shuffle the pool or drop questions from it without affecting the original RoundData object
		dataController.TrackQuestionsAnswers(questionPool.Count);

		powerUpController = this.gameObject.GetComponent<PowerUpController>();

		// questionClock = new QuestionClock(dataController.GetComponent<DataController>().RetrieveQuiz().GetQuestionData().QuestionTime);
		questionClock = new QuestionClock(30);
		quizClock = new QuizClock(0);

		UpdateTimeRemainingDisplay(questionClock);
		playerScore = 0;
		questionIndex = 0;
		rightAnswers = 0;

		questionNumberTextController.GetComponent<QuestionNumberController>().SetMaxQuestions(questionPool.Count);

		// TO DO: Colocar na Cena de carregar o quiz (deve vir antes)
		Randomizer.RandomizeAlternatives(questionPool);

		ShowQuestion();
		ShowQuestionNumber();

		jsonController.startTime = System.DateTime.Now.ToString();		// records the current system time and date
		isRoundActive = true;
	}

	void Update()
	{
		if (isRoundActive && !isQuestionAnswered)
		{
			quizClock.IncreaseTime(Time.deltaTime);
			if (!powerUpController.timeFreeze)
				questionClock.DecreaseTime(Time.deltaTime);
			UpdateTimeRemainingDisplay(questionClock);

			if (questionClock.Time <= 0f)                                                        // If timeRemaining is 0 or less, the round ends
			{
				if (questionIndex == questionPool.Count - 1)
				{
					EndRound();
				}
				else
				{
					AnswerButtonClicked(false, -1);
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

			answerButton.SetUp(question.Alternatives[i], i);                           // Pass the AnswerData to the AnswerButton so the AnswerButton knows what text to display and whether it is the correct answer
		}

		eventManager.SetAlternativesReference(answerButtonGameObjects);
	}

	public void ShowQuestionNumber()
	{
		questionNumberTextController.GetComponent<QuestionNumberController>().FormatedDisplay();
	}

	void RemoveAnswerButtons()
	{
		while (answerButtonGameObjects.Count > 0)                                           // Return all spawned AnswerButtons to the object pool
		{
			answerButtonObjectPool.ReturnObject(answerButtonGameObjects[0]);
			answerButtonGameObjects.RemoveAt(0);
		}
	}

	public void AnswerButtonClicked(bool isCorrect, int alternativeNumber)
	{
		if (powerUpController.leafImmunity == false)
        {
			isQuestionAnswered = true;
        }


		dataController.GetQuestionAnswers().RegisterPlayerAnswer(
				eventManager,
				isQuestionAnswered,
				isCorrect,
				questionIndex,
				alternativeNumber,
				questionClock.Time
			);

		powerUpController.AnswerCount(isCorrect);
		answers = GameObject.FindGameObjectsWithTag("Answer");

		if (isCorrect)
		{
			playerScore += currentRoundData.CurrentPoints;                    // If the AnswerButton that was clicked was the correct answer, add points
			scoreDisplay.text = playerScore.ToString();
			jsonController.rightAnswers++;
			streak++;
			// Add correct feedback audio
			audioSource.PlayOneShot(audioClips[(int)Clip.correct]);
		}
		else
		{
			jsonController.wrongAnswers++;
			// Add wrong feedback audio
			audioSource.PlayOneShot(audioClips[(int)Clip.wrong]);
		}

		if (streak > jsonController.streak)
		{
			jsonController.streak = streak;
		}
		string[] parts = source.Split('-');													// Separates the source string, which will be used to create "sequencia_atuacao"
		parts[1] = (questionIndex + 1).ToString().PadLeft(2, '0');
		parts[8] = (alternativeNumber + 1).ToString();										
		parts[10] = System.Math.Round(25 - questionClock.Time).ToString().PadLeft(3, '0');	// This edits each part of the string
		parts[12] = System.Convert.ToByte(isCorrect).ToString();
		string hab = "";
		if (jsonController.hab1 != h1)                                                      // Checks if any power ups have been used during this round
		{
			h1 = jsonController.hab1;
			hab = "1";
			parts[5] = powerUpController.randomIndex[0].ToString();
			parts[6] = powerUpController.randomIndex[1].ToString();
		}
		else if (jsonController.hab2 != h2)
		{
			h2 = jsonController.hab2;
			hab = "2";
		}
		else if (jsonController.hab3 != h3)
		{
			h3 = jsonController.hab3;
			hab = "3";
		}
		else
		{
			hab = "0";
		}
		parts[4] = hab;

		sequencia_atuacao.Add(string.Join("", parts));										// This puts every part of the separated source string together, creating a new string which will be saved in the .json file.

		StartCoroutine(VisualFeedback(isCorrect, alternativeNumber));
	}

	private void UpdateTimeRemainingDisplay(QuestionClock clock)
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

		string folderPath = currentRoundData.FolderPath + Path.AltDirectorySeparatorChar + DataManagementConstant.PlayerQuizDataFile;

		// Creating file with quiz results
		if (File.Exists(folderPath))
		{
			// Making sure there's only one file at one point in time
			File.Delete(folderPath); 
		}
		jsonController.score = playerScore;
		jsonController.sequencia_atuacao = sequencia_atuacao;

		dataController.WriteOnPath(currentRoundData.FolderPath + Path.AltDirectorySeparatorChar + DataManagementConstant.PlayerQuizDataFile,
			jsonController.SaveToString());


		// StreamWriter writer = File.CreateText(Application.persistentDataPath + Path.AltDirectorySeparatorChar + DataManagementConstant.PlayerDataPath);
		// writer.WriteLine(jsonController.SaveToString());
		// writer.Close();

		Debug.Log("Total time: " + quizClock.HHmmss());
		SceneManager.LoadScene("QuizResult");
	}

	public void ReturnToMenu()
	{
		SceneManager.LoadScene("MenuScreen");
	}

	IEnumerator VisualFeedback(bool isCorrect, int alternativeNumber)
	{
		if (isCorrect)
		{
			feedbackImage.GetComponent<Image>().sprite = correctAnswerIcon;
			rightAnswers++;
			powerUpController.leafImmunity = false;
		}
		else
		{
			feedbackImage.GetComponent<Image>().sprite = wrongAnswerIcon;
			streak = 0;
		}

		feedbackImage.gameObject.SetActive(true);
		yield return new WaitForSeconds(3);
		feedbackImage.gameObject.SetActive(false);

		if (powerUpController.leafImmunity == false)
        {
			if (questionPool.Count > questionIndex + 1)                                         // If there are more questions, show the next question
			{
				questionIndex++;
				questionNumberTextController.GetComponent<QuestionNumberController>().NextQuestion();
				// questionClock.NewCountdown(dataController.GetComponent<DataController>().RetrieveQuiz().GetQuestionData().QuestionTime);
				questionClock.NewCountdown(30);
				ShowQuestion();

				ShowQuestionNumber();
				isQuestionAnswered = false;
				eventManager.questionDone();

				answers[disabledAlternative].GetComponent<Image>().enabled = true;
				answers[disabledAlternative].GetComponent<Button>().enabled = true;
				answers[disabledAlternative].transform.GetChild(0).gameObject.SetActive(true);
			}
			else                                                                             // If there are no more questions, the round ends
			{
				EndRound();
			}
		}
        else
        {
			eventManager.idleState = true;
			answers[alternativeNumber].GetComponent<Image>().enabled = false;
			answers[alternativeNumber].GetComponent<Button>().enabled = false;
			answers[alternativeNumber].transform.GetChild(0).gameObject.SetActive(false);
            disabledAlternative = alternativeNumber;
			powerUpController.leafImmunity = false;
		}
	}
}