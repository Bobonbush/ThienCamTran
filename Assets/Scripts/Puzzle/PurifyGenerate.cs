using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class PurifyGenerate : MonoBehaviour
{
    [SerializeField] private GameObject circlePrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public enum AnimationType { RightWrong, dissapear };

    [SerializeField]
    private AnimationType animationType = AnimationType.RightWrong;

    [SerializeField]

    private float duration = 0.25f;
    public event Action OnPuzzleCompleted;
    public event Action OnTimeReset;

    bool mutex = false;

    


    

    
  
    private List<GameObject> ob = new List<GameObject>();
    private List<int> gen_code = new List<int>();

    // 0 = up
    // 1 = right
    // 2 = down
    // 3 = left

    private int cnt = 0;
    // use full 0 for fixed sprite render

    int stillInAnimation = 0;
    bool errorAnimation = false; 

    

    public void GeneratePurify(List<int> code)
    {
        gen_code = code;
        for(int i = 0; i < code.Count; i++)
        {
            
            ob.Add(Instantiate(circlePrefab, transform));
            ob[i].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -90.0f * (code[i] - 1));
        }
    }

    public void GeneratePurify(int cnt)
    {
        for (int i = 0; i < cnt; i++)
        {
            ob.Add(Instantiate(circlePrefab, transform));
        }
    }

    public void ReceiveInput(Vector2 moveInput)
    {
        
        if(errorAnimation || cnt >= gen_code.Count)
        {
            return;
        }
        

        int type = 0;
        if (moveInput.x > 0)
        {
            type = 1;
        }
        if (moveInput.x < 0)
        {
            type = 3;
        }
        if (moveInput.y < 0)
        {
            type = 2;
        }


        if (type == gen_code[cnt])
        {
            StartCoroutine(Correct());
        }
        else StartCoroutine(Wrong());
    }


    IEnumerator ZoomInAndDissapearffect(int index)
    {
        
        Image image = ob[index].GetComponent<Image>();
        RectTransform rect = ob[index].GetComponent<RectTransform>();


        Color startColor = image.color;
        Vector3 startScale = rect.localScale;

        Color endColor = Color.white;
        endColor.a = 0f;

        Vector3 endScale = Vector3.zero;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float p = Mathf.SmoothStep(0f, 1f, t / duration);

            image.color = Color.Lerp(startColor, endColor, p);
            rect.localScale = Vector3.Lerp(startScale, endScale, p);

            yield return null;
        }


    }

    private IEnumerator AcquireLock()
    {
        while (mutex)
            yield return null;

        mutex = true;
    }

    private void ReleaseLock()
    {
        mutex = false;
    }


    public IEnumerator Correct()
    {
        int index = cnt;
        cnt++;
        if(cnt == ob.Count)
        {
            OnTimeReset?.Invoke();
        }

        stillInAnimation++;
        if (animationType == AnimationType.RightWrong)
        {
            yield return ZoomInAndDissapearffect(index);
        }

        if(animationType == AnimationType.dissapear)
        {
            yield return ZoomOutandPaybackEffect(index, Color.green, true);
        }





        yield return AcquireLock();
        stillInAnimation--;
        ReleaseLock();

        if (stillInAnimation > 0) yield break;

        if (cnt == ob.Count)
        {
            OnPuzzleCompleted?.Invoke();
        }

        
        yield return null;
        
    }

    IEnumerator ZoomOutandPaybackEffect(int index, Color targetColors, bool keepColor = false)
    {
        Image image = ob[index].GetComponent<Image>();
        RectTransform rect = ob[index].GetComponent<RectTransform>();


        Color startColor = image.color;
        Vector3 startScale = rect.localScale;

        Color endColor = targetColors;
        if (!keepColor)
            endColor.a = 0f;
        else
            endColor.a = 0.5f; ;

        Vector3 endScale = startScale * 1.2f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float p = Mathf.SmoothStep(0f, 1f, t / duration);

            image.color = Color.Lerp(startColor, endColor, p);
            
            rect.localScale = Vector3.Lerp(startScale, endScale, p);

            yield return null;
        }

        t = 0f;
        
        while (t < duration)
        {
            t += Time.deltaTime;

            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            if (!keepColor)
                image.color = Color.Lerp(endColor, startColor, p);
            
            rect.localScale = Vector3.Lerp(endScale, startScale, p);

            yield return null;
        }


        
    }

    private IEnumerator Wrong()
    {
        errorAnimation = true;

        if (animationType == AnimationType.dissapear)
        {
            
        }

        if (animationType == AnimationType.RightWrong)
        {
            yield return  ZoomOutandPaybackEffect(cnt, Color.red);
        }

        errorAnimation = false;


    }

    public bool IsDone()
    {
        return cnt == ob.Count;
    }

    public void Clear()
    {
        cnt = 0;
        gen_code.Clear();
        errorAnimation = false;
        for(int i = 0;  i< ob.Count; i++)
        {
            if (ob[i] != null)
                 Destroy(ob[i].gameObject);
        }
        ob.Clear();
        
    }

    

   

    
    
}
