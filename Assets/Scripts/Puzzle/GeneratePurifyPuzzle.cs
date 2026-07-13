using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.UI;
public class GeneratePurifyPuzzle : MonoBehaviour
{

    [SerializeField] private GameObject PuzzleSpace;
    [SerializeField] private GameObject RoundSpace;

    [SerializeField] private Slider Timingbar1;
    [SerializeField] private Slider Timingbar2;

    public event Action OnPuzzleCompleted;
    public event Action OnPuzzleFail;

    private int round = 0;
    PurifyGenerate roundGen;
    PurifyGenerate puzzle;
    

    private int finishCnt = 0;

    float duration = 0.0f;

    bool stopCheckTime = false;

    private void Awake()
    {
        if (RoundSpace != null)
        {
            roundGen = RoundSpace.GetComponent<PurifyGenerate>();
        }

        if (PuzzleSpace != null)
        {
            puzzle = PuzzleSpace.GetComponent<PurifyGenerate>();
        }

        if (puzzle != null)
        {
            puzzle.OnPuzzleCompleted += HandlePuzzleCompleted;
            puzzle.OnTimeReset += HandleTimePauseWhenFinish;
        }

        if (roundGen != null)
        {
            roundGen.OnPuzzleCompleted += HandleRoundCompleted;
        }
    }



    private void Update()
    {
        if (Timingbar1 == null || Timingbar2 == null)
        {
            return;
        }

        if (!stopCheckTime)
        {
            if (duration > 0.0f)
            {
                duration = Math.Max(0, duration - Time.deltaTime);
                Timingbar1.value = duration;
                Timingbar2.value = duration;
            }
            else
            {

                StopAllCoroutines();
                OnPuzzleFail?.Invoke();
                

            }
        }
    }


    // Wait for animation to actually complete
    private void HandleTimePauseWhenFinish()
    {
        stopCheckTime = true;
    }

    private void HandlePuzzleCompleted()
    {
        finishCnt++;

        puzzle.Clear();

        StartCoroutine(roundGen.Correct());
        if (finishCnt < round)
        {
            ResetTiming();
            stopCheckTime = false;
            GeneratePurify();
        }
    }

    private void HandleRoundCompleted()
    {
        OnPuzzleCompleted?.Invoke();
    }
    private void Start()
    {
        if (roundGen == null && RoundSpace != null)
        {
            roundGen = RoundSpace.GetComponent<PurifyGenerate>();
        }

        if (puzzle == null && PuzzleSpace != null)
        {
            puzzle = PuzzleSpace.GetComponent<PurifyGenerate>();
        }
    }

    public void Generate(int round_cnt, float _duration)
    {
        if (roundGen == null || puzzle == null || Timingbar1 == null || Timingbar2 == null)
        {
            return;
        }

        finishCnt = 0;
        stopCheckTime = false;
        SetTiming(_duration);
        round = round_cnt;
        roundGen.Clear();
        roundGen.GeneratePurify(round_cnt);

        

        GeneratePurify();
        
    }

    private void GeneratePurify()
    {
        if (puzzle == null)
        {
            return;
        }

        List<int> code = new List<int>();
        puzzle.Clear();
       
        for(int i = 0; i < 4; i++)
        {
            code.Add(UnityEngine.Random.Range(0, 4));
        }

        puzzle.GeneratePurify(code);
    }



    public void Purify(Vector2 input)
    {
        if (puzzle == null)
        {
            return;
        }

        puzzle.ReceiveInput(input);
    }

    private void SetTiming(float _duration)
    {
        duration = _duration;
        Timingbar1.minValue = 0.0f;
        Timingbar1.maxValue = duration;

        Timingbar2.minValue = 0.0f;
        Timingbar2.maxValue = duration;
    }

    private void ResetTiming()
    {
        duration = Timingbar1.maxValue;

    }
}
