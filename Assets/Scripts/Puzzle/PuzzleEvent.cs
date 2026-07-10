using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface PuzzleEvent
{
    // 0 not activate 1 is activating 2 is done
    public int Status { get; }

    public void Trigger();
    public void ShowInteraction();

    public int GetStatus();
}

