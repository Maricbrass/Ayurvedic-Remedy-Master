using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string question;
        public string[] options;
        public int correctAnswerIndex;
    }

    [Header("Quiz Settings")]
    [Tooltip("How many random questions to select from the question bank")]
    public int numberOfQuestionsToShow = 10;
    [Tooltip("Path to questions JSON file in Resources folder")]
    public string questionsJsonPath = "questions";
    
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public GameObject resultPanel;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultMessageText;
    public Button mainMenuButton;
    
    [Header("Settings")]
    public float delayBetweenQuestions = 1.5f;
    public string mainMenuSceneName = "MainMenu";
    public Color correctAnswerColor = new Color(0.2f, 0.8f, 0.2f);
    public Color wrongAnswerColor = new Color(0.8f, 0.2f, 0.2f);
    public Color defaultButtonColor = Color.white;

    private Question[] allQuestions; // All questions loaded from JSON
    private Question[] questions;    // Random selection of questions for this quiz
    private int currentQuestionIndex;
    private int score = 0;
    private ColorBlock defaultColorBlock;

    void Start()
    {
        // Save the default button color
        if (optionButtons.Length > 0)
        {
            defaultColorBlock = optionButtons[0].colors;
        }
        
        // Hide the result panel initially
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // Setup main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // Load questions from JSON
        LoadQuestionsFromJson();
        
        // Start the quiz
        currentQuestionIndex = 0;
        score = 0;
        ShowQuestion();
    }

    void LoadQuestionsFromJson()
    {
        // Load the JSON file from Resources
        TextAsset questionsJson = Resources.Load<TextAsset>(questionsJsonPath);
        
        if (questionsJson != null)
        {
            // Parse the JSON
            string jsonStr = questionsJson.text;
            
            // Remove any comments in the JSON if present (like the // filepath comments)
            if (jsonStr.Contains("//"))
            {
                string[] lines = jsonStr.Split('\n');
                string cleanJson = "";
                foreach (string line in lines)
                {
                    if (!line.TrimStart().StartsWith("//"))
                    {
                        cleanJson += line + "\n";
                    }
                }
                jsonStr = cleanJson;
            }
            
            // Parse the JSON as an array
            allQuestions = JsonUtility.FromJson<Wrapper>("{\"items\":" + jsonStr + "}").items;
            
            Debug.Log($"Loaded {allQuestions.Length} questions from JSON");
            
            // Select random questions
            SelectRandomQuestions();
        }
        else
        {
            Debug.LogError($"Questions JSON file not found at: {questionsJsonPath}");
            
            // Create some default questions as fallback
            allQuestions = new Question[]
            {
                new Question { 
                    question = "What does Ayurveda mean?", 
                    options = new string[] { "Knowledge of Life", "Science of Food", "Healing through Yoga", "Art of Living" },
                    correctAnswerIndex = 0
                },
                new Question { 
                    question = "Which dosha is associated with fire and water?", 
                    options = new string[] { "Vata", "Pitta", "Kapha", "Agni" },
                    correctAnswerIndex = 1
                },
                new Question { 
                    question = "File not found. Is this a test question?", 
                    options = new string[] { "Yes", "No", "Maybe", "I don't know" },
                    correctAnswerIndex = 0
                }
            };
            
            Debug.Log("Using fallback questions");
            SelectRandomQuestions();
        }
    }
    
    // Helper class for JSON deserialization
    [System.Serializable]
    private class Wrapper
    {
        public Question[] items;
    }
    
    void SelectRandomQuestions()
    {
        // Make sure we don't try to select more questions than exist
        int count = Mathf.Min(numberOfQuestionsToShow, allQuestions.Length);
        
        // Create a copy of all questions to shuffle
        List<Question> questionPool = new List<Question>(allQuestions);
        List<Question> selectedQuestions = new List<Question>();
        
        // Randomly select the specified number of questions
        for (int i = 0; i < count; i++)
        {
            if (questionPool.Count == 0)
                break;
                
            int randomIndex = Random.Range(0, questionPool.Count);
            selectedQuestions.Add(questionPool[randomIndex]);
            questionPool.RemoveAt(randomIndex);
        }
        
        // Set the questions for this quiz
        questions = selectedQuestions.ToArray();
        Debug.Log($"Selected {questions.Length} random questions for this quiz");
    }

    void ShowQuestion()
    {
        // Safety check for index out of range
        if (currentQuestionIndex >= questions.Length || questions.Length == 0)
        {
            ShowResults();
            return;
        }
        
        // Reset all buttons to default state
        foreach (var btn in optionButtons)
        {
            btn.interactable = true;
            ColorBlock cb = btn.colors;
            cb.normalColor = defaultButtonColor;
            cb.selectedColor = defaultButtonColor;
            btn.colors = cb;
        }
        
        // Display the current question
        Question q = questions[currentQuestionIndex];
        questionText.text = $"Question {currentQuestionIndex + 1}/{questions.Length}:\n{q.question}";

        // Update button options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                int index = i; // Capture the index for the lambda
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }
            else
            {
                // Hide unused buttons
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnAnswerSelected(int index)
    {
        // Disable all buttons to prevent multiple selections
        foreach (var btn in optionButtons)
        {
            btn.interactable = false;
        }
        
        // Check if answer is correct
        bool isCorrect = index == questions[currentQuestionIndex].correctAnswerIndex;
        if (isCorrect) 
        {
            score++;
            
            // Change the button color to indicate correct answer
            ColorBlock cb = optionButtons[index].colors;
            cb.normalColor = correctAnswerColor;
            cb.selectedColor = correctAnswerColor;
            optionButtons[index].colors = cb;
        }
        else
        {
            // Change the selected button to indicate wrong answer
            ColorBlock selectedCb = optionButtons[index].colors;
            selectedCb.normalColor = wrongAnswerColor;
            selectedCb.selectedColor = wrongAnswerColor;
            optionButtons[index].colors = selectedCb;
            
            // Highlight the correct answer
            int correctIndex = questions[currentQuestionIndex].correctAnswerIndex;
            ColorBlock correctCb = optionButtons[correctIndex].colors;
            correctCb.normalColor = correctAnswerColor;
            correctCb.selectedColor = correctAnswerColor;
            optionButtons[correctIndex].colors = correctCb;
        }
        
        // Automatically go to next question after delay
        StartCoroutine(AutoAdvanceAfterDelay());
    }
    
    IEnumerator AutoAdvanceAfterDelay()
    {
        yield return new WaitForSeconds(delayBetweenQuestions);
        
        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Length)
        {
            ShowQuestion();
        }
        else
        {
            ShowResults();
        }
    }
    
    void ShowResults()
    {
        // Hide question UI
        questionText.gameObject.SetActive(false);
        foreach (var btn in optionButtons)
        {
            btn.gameObject.SetActive(false);
        }
        
        // Calculate percentage score
        float percentage = (float)score / questions.Length * 100;
        
        // Show result panel
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            // Update score text
            if (resultScoreText != null)
            {
                resultScoreText.text = $"Score: {score}/{questions.Length} ({percentage:F0}%)";
            }
            
            // Update congratulatory message based on score
            if (resultMessageText != null)
            {
                if (percentage >= 90)
                {
                    resultMessageText.text = "Outstanding! You're an Ayurvedic Master!";
                }
                else if (percentage >= 70)
                {
                    resultMessageText.text = "Great job! You know your Ayurvedic remedies well!";
                }
                else if (percentage >= 50)
                {
                    resultMessageText.text = "Good effort! You're on your way to understanding Ayurveda.";
                }
                else
                {
                    resultMessageText.text = "Keep learning! Ayurveda has much to teach.";
                }
            }
        }
        else
        {
            Debug.LogError("Result panel is not assigned in the inspector!");
        }
    }
    
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}