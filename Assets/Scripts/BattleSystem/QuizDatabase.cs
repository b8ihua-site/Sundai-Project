using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class QuizQuestion
{
    public string question;   // 問題文
    public string answer;     // ひらがなの答え（例：ちゅうおうせん）
}

[System.Serializable]
public class SubjectQuizData
{
    public string subjectName;  // 科目名（例：市ヶ谷）
    public List<QuizQuestion> questions;
}

[CreateAssetMenu(fileName = "QuizDatabase", menuName = "Battle/QuizDatabase")]
public class QuizDatabase : ScriptableObject
{
    public List<SubjectQuizData> subjects;

    public SubjectQuizData GetSubject(string name)
    {
        return subjects.Find(s => s.subjectName == name);
    }

    public QuizQuestion GetRandomQuestion(string subjectName)
    {
        var subject = GetSubject(subjectName);
        if (subject == null || subject.questions.Count == 0) return null;
        return subject.questions[Random.Range(0, subject.questions.Count)];
    }
}