using System.Collections;

// add this interface to ANY selected object to run extra animation logic
// (pipe enter, brick bounce, parenting to pipeContainer, VFX, etc.)
public interface IFileSelectSequence
{
    IEnumerator Play(FileSelectSequenceContext ctx);
}