using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenEffects : MonoBehaviour
{
    //============================
    //SCENE MUST HAVE EVENT SYSTEM
    //============================

    private static Canvas targetCanvas;
    private static bool effectActive;
    private static MonoBehaviour currentlyRunningEffectOnInstance;

    private static void Initialise()
    {
        //If no canvas exists create one
        if (targetCanvas == null)
        {
            GameObject gameObject = new GameObject
            {
                name = "Target Canvas"
            };
            targetCanvas = gameObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public static void Clear()
    {
        if (targetCanvas != null && !effectActive)
        {
            foreach (Transform child in targetCanvas.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    //==============================
    //AXIAL TILING
    //==============================

    public static void AxialTiling(Color colorTint, int numberOfRows, float betweenTileDelay,  MonoBehaviour runOnInstance, bool fillColumnsFirst = false, bool startLeft = true, bool startBottom = true, RawImage tilePrefab = null)
    {
        if (!effectActive)
        {
            Initialise();
            Clear();
            effectActive = true;
            currentlyRunningEffectOnInstance = runOnInstance;
            runOnInstance.StartCoroutine(AxialTilingCor(colorTint, numberOfRows, betweenTileDelay, runOnInstance, fillColumnsFirst, startLeft, startBottom, tilePrefab));
        }
        else
        {
            Debug.LogWarning("AxialTiling() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator AxialTilingCor(Color colorTint, int numberOfRows, float betweenTileDelay, MonoBehaviour runOnInstance, bool fillColumnsFirst, bool startLeft, bool startBottom, RawImage tilePrefab)
    {
        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //Compute width of a single tile
        float tileWidth = extents.y / numberOfRows;

        //Compute number of columns with one extra to make sure screen is filled
        int numberOfColumns = Mathf.FloorToInt(extents.x / tileWidth) + 1;

        //If no tile specified create one
        if (tilePrefab == null)
        {
            tilePrefab = CreateImagePrefab();
        }

        //Configure tile
        SetImageColor(tilePrefab, colorTint);
        SetImageSize(tilePrefab, tileWidth, tileWidth);
        SetImageAnchor(tilePrefab, startLeft, startBottom);
        SetImagePivot(tilePrefab, 0.5f, 0.5f);

        //Iterate either as 'for every column, for every row' or 'for every row, for every column'
        int iMax = fillColumnsFirst ? numberOfColumns : numberOfRows;
        int jMax = fillColumnsFirst ? numberOfRows : numberOfColumns;
        for (int i = 0; i < iMax; i++)
        {
            for (int j = 0; j < jMax; j++)
            {
                //Instantiate from prefab
                RawImage newImage = Instantiate(tilePrefab, targetCanvas.transform);

                //Play SFX here

                //Make sure active
                newImage.gameObject.SetActive(true);

                //Steps measured in units of tileWidth/2
                int xStep;
                int yStep;

                if (fillColumnsFirst)
                {
                    //i is horizontal, j is vertical
                    xStep = 2 * (startLeft ? i : iMax - 1 - i) + 1;
                    yStep = 2 * (startBottom ? j : jMax - 1 - j) + 1;
                }
                else
                {
                    //i is vertical, j is horizontal
                    xStep = 2 * (startLeft ? j : jMax - 1 - j) + 1;
                    yStep = 2 * (startBottom ? i : iMax - 1 - i) + 1;
                }

                //Move image into position
                newImage.rectTransform.position = new Vector3(xStep * tileWidth / 2, yStep * tileWidth / 2);

                //Start enter effect
                runOnInstance.StartCoroutine(EnterEffect(newImage));

                //Wait
                if (betweenTileDelay > 0)
                {
                    yield return new WaitForSeconds(betweenTileDelay);
                }
            }                        
        }

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //DIAGONAL TILING
    //==============================

    public static void DiagonalTiling(Color colorTint, int numberOfRows, float betweenTileDelay, MonoBehaviour runOnInstance, bool startLeft = true, bool startBottom = true, RawImage tilePrefab = null)
    {
        if (!effectActive)
        {
            currentlyRunningEffectOnInstance = runOnInstance;
            Initialise();
            Clear();
            effectActive = true;
            runOnInstance.StartCoroutine(DiagonalTilingCor(colorTint, numberOfRows, betweenTileDelay, runOnInstance, startLeft, startBottom, tilePrefab));
        }
        else
        {
            Debug.LogWarning("DiagonalTiling() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator DiagonalTilingCor(Color colorTint, int numberOfRows, float betweenTileDelay, MonoBehaviour runOnInstance, bool startLeft, bool startBottom, RawImage tilePrefab)
    {
        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //Compute width of a single tile
        float tileWidth = extents.y / numberOfRows;

        //Compute number of columns with one extra to make sure screen is filled
        int numberOfColumns = Mathf.FloorToInt(extents.x / tileWidth) + 1;

        //If no tile specified create one
        if (tilePrefab == null)
        {
            tilePrefab = CreateImagePrefab();
        }

        //Configure tile
        SetImageColor(tilePrefab, colorTint);
        SetImageSize(tilePrefab, tileWidth, tileWidth);
        SetImageAnchor(tilePrefab, startLeft, startBottom);
        SetImagePivot(tilePrefab, 0.5f, 0.5f);

        //Iterate through list of coords which move over grid diagonally, so (1,1), (2,1), (1,2), (1,3), (2,2), (3, 1) like 
        //      (1, 3) 
        //       |    \
        //   (1, 2)   (2, 2)
        //         \       \
        // (1, 1) -- (2, 1)  (3, 1) -- ...
        List<Vector2Int> diagonalCoords = DiagonallyTileRectangle(numberOfColumns, numberOfRows);
        for (int i = 0; i < diagonalCoords.Count; i++)
        {
            //Instantiate from prefab
            RawImage newImage = Instantiate(tilePrefab, targetCanvas.transform);

            //Play SFX here

            //Make sure active
            newImage.gameObject.SetActive(true);

            //Get i'th diagonal coord
            Vector2Int diagonalCoord = diagonalCoords[i];

            //Shift to start from (0, 0), steps measured in units of tileWidth/2
            int xStep = diagonalCoord.x - 1;
            int yStep = diagonalCoord.y - 1;
            xStep = 2 * (startLeft ? xStep : numberOfColumns - 1 - xStep) + 1;
            yStep = 2 * (startBottom ? yStep : numberOfRows - 1 - yStep) + 1;

            //Move image into position
            newImage.rectTransform.position = new Vector3(xStep * tileWidth / 2, yStep * tileWidth / 2);

            //Start enter effect
            runOnInstance.StartCoroutine(EnterEffect(newImage));

            //Wait
            if (betweenTileDelay > 0)
            {
                yield return new WaitForSeconds(betweenTileDelay);
            }
        }

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //HORIZONTAL BANNERS
    //==============================

    public static void HorizontalBanners(Color colorTint, int numberOfBanners, float delayBetweenBanners, float acrossTimeForOneBanner, MonoBehaviour runOnInstance, bool startLeft = true, bool entering = true, bool startTop = false, RawImage bannerPrefab = null, AnimationCurve animCurve = null)
    {
        if (!effectActive)
        {
            currentlyRunningEffectOnInstance = runOnInstance;
            Initialise();
            Clear();
            effectActive = true;
            runOnInstance.StartCoroutine(HorizontalBannersCor(colorTint, numberOfBanners, delayBetweenBanners, acrossTimeForOneBanner, startLeft, entering, startTop, bannerPrefab, animCurve));
        }
        else
        {
            Debug.LogWarning("HorizontalBanners() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator HorizontalBannersCor(Color colorTint, int numberOfBanners, float delayBetweenBanners, float acrossTimeForOneBanner, bool startLeft, bool entering, bool startTop, RawImage bannerPrefab, AnimationCurve animCurve = null)
    {
        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //Compute height of a single banner
        float bannerHeight = extents.y / numberOfBanners;

        //If no banner specified create one
        if (bannerPrefab == null)
        {
            bannerPrefab = CreateImagePrefab();
        }

        //Configure banner
        SetImageColor(bannerPrefab, colorTint);
        SetImageSize(bannerPrefab, extents.x, bannerHeight);
        SetImageAnchor(bannerPrefab, false, true);
        SetImagePivot(bannerPrefab, startLeft ? 1 : 0, startTop ? 1 : 0);

        //List of all banners
        List<RawImage> allBanners = new List<RawImage>();
        List<Vector2> initialBannerPositions = new List<Vector2>();
        List<Vector2> finalBannerPositions = new List<Vector2>();
        List<float> changeInPosMagnitude = new List<float>();

        //List of banners that have started moving
        List<RawImage> movingBanners = new List<RawImage>();

        //Iterate over each banner
        for (int i = 0; i < numberOfBanners; i++)
        {
            //Instantiate from prefab
            RawImage newImage = Instantiate(bannerPrefab, targetCanvas.transform);

            //Add to list of banners
            allBanners.Add(newImage);

            //Play SFX here

            //Make sure active
            newImage.gameObject.SetActive(true);

            //Set initial position
            if (entering)
            {
                Vector2 initialPos = new Vector2(startLeft ? 0 : extents.x, startTop ? (extents.y - (i * bannerHeight)) : (i * bannerHeight));
                newImage.rectTransform.position = initialPos;
                Vector2 finalPos = new Vector2(startLeft ? extents.x : 0, initialPos.y);
                initialBannerPositions.Add(initialPos);
                finalBannerPositions.Add(finalPos);
                changeInPosMagnitude.Add((initialPos - finalPos).magnitude);
            }
            else
            {
                Vector2 initialPos = new Vector2(startLeft ? extents.x : 0, startTop ? (extents.y - (i * bannerHeight)) : (i * bannerHeight));
                newImage.rectTransform.position = initialPos;
                Vector2 finalPos = new Vector2(startLeft ? 0 : extents.x, allBanners[i].rectTransform.position.y);
                initialBannerPositions.Add(initialPos);
                finalBannerPositions.Add(finalPos);
                changeInPosMagnitude.Add((initialPos - finalPos).magnitude);
            }
        }

        //Add first banner to moving banners
        movingBanners.Add(allBanners[0]);

        float elapsedTime = 0f;
        int previousMult = 0;
        float totalCurveTime = (animCurve == null) ? 0 : animCurve.keys[animCurve.length - 1].time;

        //Total time is the time until the last banner reaches the end, so is the number of delays plus the time taken for one banner to cross
        float totalTime = (numberOfBanners - 1) * delayBetweenBanners + acrossTimeForOneBanner;

        while (elapsedTime < totalTime)
        {
            //If enough time has elapsed before the (previousMult + 1)'th banner and there is still another banner yet to start moving, increment previous mult and add next moving banner
            if (((elapsedTime / delayBetweenBanners) >= previousMult + 1) && (previousMult < allBanners.Count - 1))
            {
                previousMult += 1;
                movingBanners.Add(allBanners[previousMult]);
            }

            //Lerp those banners that are moving between their initial and final positions
            //Shift elapsed time back by the time before the i'th banner, then take that as a proportion of the time taken for one banner to cross
            for (int i = 0; i < movingBanners.Count; i++)
            {
                if (animCurve == null)
                {
                    movingBanners[i].rectTransform.position = Vector2.Lerp(initialBannerPositions[i], finalBannerPositions[i], (elapsedTime - i * delayBetweenBanners) / acrossTimeForOneBanner);
                }
                else
                {
                    float curveT = animCurve.Evaluate(totalCurveTime * (elapsedTime - i * delayBetweenBanners) / acrossTimeForOneBanner);

                    //Scaled to go from 0 to 1
                    curveT = (curveT - animCurve.keys[0].value) / (animCurve.keys[animCurve.length - 1].value - animCurve.keys[0].value);

                    //Debug.Log((finalBannerPositions[i] + initialBannerPositions[i]).normalized);
                    movingBanners[i].rectTransform.position = initialBannerPositions[i] + (finalBannerPositions[i] - initialBannerPositions[i]).normalized * changeInPosMagnitude[i] * curveT;


                }
            }

            //Increment elapsedTime
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        //Set final positions
        foreach (RawImage image in movingBanners)
        {
            if (!entering)
            {
                image.rectTransform.position = new Vector3(startLeft ? 0 : extents.x, image.rectTransform.position.y);
            }
            else
            {
                image.rectTransform.position = new Vector3(startLeft ? extents.x : 0, image.rectTransform.position.y);
            }
        }

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //VERTICAL BANNERS
    //==============================

    public static void VerticalBanners(Color colorTint, int numberOfBanners, float delayBetweenBanners, float acrossTimeForOneBanner, MonoBehaviour runOnInstance, bool startTop = true, bool isEntering = true, bool startLeft = true, RawImage bannerPrefab = null, AnimationCurve animCurve = null)
    {
        if (!effectActive)
        {
            currentlyRunningEffectOnInstance = runOnInstance;
            Initialise();
            Clear();
            effectActive = true;
            runOnInstance.StartCoroutine(VerticalBannersCor(colorTint, numberOfBanners, delayBetweenBanners, acrossTimeForOneBanner, startTop, isEntering, startLeft, bannerPrefab, animCurve));
        }
        else
        {
            Debug.LogWarning("VerticalBanners() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator VerticalBannersCor(Color colorTint, int numberOfBanners, float delayBetweenBanners, float acrossTimeForOneBanner, bool startTop, bool isEntering, bool startLeft, RawImage bannerPrefab, AnimationCurve animCurve = null)
    {
        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //Compute height of a single banner
        float bannerWidth = extents.x / numberOfBanners;

        //If no banner specified create one
        if (bannerPrefab == null)
        {
            bannerPrefab = CreateImagePrefab();
        }

        //Configure banner
        SetImageColor(bannerPrefab, colorTint);
        SetImageSize(bannerPrefab, bannerWidth, extents.y);
        SetImageAnchor(bannerPrefab, true, false);
        SetImagePivot(bannerPrefab, startLeft ? 0 : 1, startTop ? 0 : 1);

        //List of all banners
        List<RawImage> allBanners = new List<RawImage>();
        List<Vector2> initialBannerPositions = new List<Vector2>();
        List<Vector2> finalBannerPositions = new List<Vector2>();
        List<float> changeInPosMagnitude = new List<float>();

        //List of banners that have started moving
        List<RawImage> movingBanners = new List<RawImage>();

        //Iterate over each banner
        for (int i = 0; i < numberOfBanners; i++)
        {
            //Instantiate from prefab
            RawImage newImage = Instantiate(bannerPrefab, targetCanvas.transform);

            //Add to list of banners
            allBanners.Add(newImage);

            //Play SFX here

            //Make sure active
            newImage.gameObject.SetActive(true);

            //Set initial position
            if (isEntering)
            {
                Vector2 initialPos = new Vector2(startLeft ? (i * bannerWidth) : (extents.x - (i * bannerWidth)), startTop ? extents.y : 0);
                newImage.rectTransform.position = initialPos;
                Vector2 finalPos = new Vector2(allBanners[i].rectTransform.position.x, startTop ? 0 : extents.y);
                initialBannerPositions.Add(initialPos);
                finalBannerPositions.Add(finalPos);
                changeInPosMagnitude.Add((initialPos - finalPos).magnitude);
            }
            else
            {
                Vector2 initialPos = new Vector2(startLeft ? (i * bannerWidth) : (extents.x - (i * bannerWidth)), startTop ? 0 : extents.y);
                newImage.rectTransform.position = initialPos;
                Vector2 finalPos = new Vector2(allBanners[i].rectTransform.position.x, startTop ? extents.y : 0);
                initialBannerPositions.Add(initialPos);
                finalBannerPositions.Add(finalPos);
                changeInPosMagnitude.Add((initialPos - finalPos).magnitude);
            }
        }

        //Add first banner to moving banners
        movingBanners.Add(allBanners[0]);

        float elapsedTime = 0f;
        int previousMult = 0;
        float totalCurveTime = (animCurve == null) ? 0 : animCurve.keys[animCurve.length - 1].time;

        //Total time is the time until the last banner reaches the end, so is the number of delays plus the time taken for one banner to cross
        float totalTime = (numberOfBanners - 1) * delayBetweenBanners + acrossTimeForOneBanner;

        while (elapsedTime < totalTime)
        {
            //If enough time has elapsed before the (previousMult + 1)'th banner and there is still another banner yet to start moving, increment previous mult and add next moving banner
            if (((elapsedTime / delayBetweenBanners) >= previousMult + 1) && (previousMult < allBanners.Count - 1))
            {
                previousMult += 1;
                movingBanners.Add(allBanners[previousMult]);
            }

            //Lerp those banners that are moving between their initial and final positions
            //Shift elapsed time back by the time before the i'th banner, then take that as a proportion of the time taken for one banner to cross
            for (int i = 0; i < movingBanners.Count; i++)
            {
                if (animCurve == null)
                {
                    movingBanners[i].rectTransform.position = Vector2.Lerp(initialBannerPositions[i], finalBannerPositions[i], (elapsedTime - i * delayBetweenBanners) / acrossTimeForOneBanner);
                }
                else
                {
                    float curveT = animCurve.Evaluate(totalCurveTime * (elapsedTime - i * delayBetweenBanners) / acrossTimeForOneBanner);

                    //Scaled to go from 0 to 1
                    curveT = (curveT - animCurve.keys[0].value) / (animCurve.keys[animCurve.length - 1].value - animCurve.keys[0].value);

                    movingBanners[i].rectTransform.position = initialBannerPositions[i] + (finalBannerPositions[i] - initialBannerPositions[i]).normalized * changeInPosMagnitude[i] * curveT;
                }
            }

            //Increment elapsedTime
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        //Set final positions
        foreach (RawImage image in movingBanners)
        {
            if (!isEntering)
            {
                image.rectTransform.position = new Vector2(image.rectTransform.position.x, startTop ? extents.y : 0);
            }
            else
            { 
                image.rectTransform.position = new Vector2(image.rectTransform.position.x, startTop ? 0 : extents.y);
            }
        }

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //BARS
    //==============================

    public static void Bars(Color colorTint, float time, float canvasProportionCovered, MonoBehaviour runOnInstance, bool isHorizontal = true, bool isEntering = true, RawImage barPrefab = null, AnimationCurve animCurve = null)
    {
        if (!effectActive)
        {
            currentlyRunningEffectOnInstance = runOnInstance;
            Initialise();
            Clear();
            effectActive = true;
            runOnInstance.StartCoroutine(BarsCor(colorTint, time, canvasProportionCovered, isHorizontal, isEntering, barPrefab, animCurve));
        }
        else
        {
            Debug.LogWarning("Bars() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator BarsCor(Color colorTint, float time, float canvasProportionCovered, bool isHorizontal, bool isEntering, RawImage barPrefab, AnimationCurve animCurve = null)
    {
        //Play SFX here

        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //If no bar specified create one
        if (barPrefab == null)
        {
            barPrefab = CreateImagePrefab();
        }

        //Configure bar
        SetImageColor(barPrefab, colorTint);
        SetImageSize(barPrefab, extents.x, extents.y);
        SetImageAnchor(barPrefab, true, true);
        SetImagePivot(barPrefab, isHorizontal ? 0.5f : 0, isHorizontal ? 0 : 0.5f);

        //Instantiate first bar from prefab
        RawImage newImage1 = Instantiate(barPrefab, targetCanvas.transform);

        //Make sure active
        newImage1.gameObject.SetActive(true);

        //Set initial and final positions
        Vector2 image1TargetPosition = isHorizontal ? new Vector2(extents.x / 2, extents.y - (canvasProportionCovered * extents.y)) : new Vector2(extents.x - (canvasProportionCovered * extents.x), extents.y / 2);
        Vector2 image1InitialPosition = isHorizontal ? new Vector2(extents.x / 2, extents.y) : new Vector2(extents.x, extents.y / 2);

        //Instantiate first bar from prefab
        RawImage newImage2 = Instantiate(barPrefab, targetCanvas.transform);

        //Make sure active
        newImage2.gameObject.SetActive(true);

        //Set initial and final positions
        Vector2 image2TargetPosition = isHorizontal ? new Vector2(extents.x / 2, -(extents.y - (canvasProportionCovered * extents.y))) : new Vector2(-(extents.x - (canvasProportionCovered * extents.x)), extents.y / 2);
        Vector2 image2InitialPosition = isHorizontal ? new Vector2(extents.x / 2, -extents.y) : new Vector2(-extents.x, extents.y / 2);

        //Lerp between initial and final positons
        float elapsedTime = 0f;
        float totalCurveTime = (animCurve == null) ? 0 : animCurve.keys[animCurve.length - 1].time;
        while (elapsedTime < time)
        {      
            Vector2 start1 = isEntering ? image1InitialPosition : image1TargetPosition;
            Vector2 end1 = isEntering ? image1TargetPosition : image1InitialPosition;
            float change1 = (start1 - end1).magnitude;
            
            Vector2 start2 = isEntering ? image2InitialPosition : image2TargetPosition;
            Vector2 end2 = isEntering ? image2TargetPosition : image2InitialPosition;
            float change2 = (start2 - end2).magnitude;

            if (animCurve == null)
            {
                newImage1.rectTransform.position = Vector2.Lerp(start1, end1, elapsedTime / time);
                newImage2.rectTransform.position = Vector2.Lerp(start2, end2, elapsedTime / time);
                
            }
            else
            {
                float curveT = animCurve.Evaluate(totalCurveTime * elapsedTime / time);

                //Scaled to go from 0 to 1
                curveT = (curveT - animCurve.keys[0].value) / (animCurve.keys[animCurve.length - 1].value - animCurve.keys[0].value);

                newImage1.rectTransform.position = start1 + (end1 - start1).normalized * (change1 * curveT);
                newImage2.rectTransform.position = start2 + (end2 - start2).normalized * (change2 * curveT);
            }

            //Increment elapsedTime
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //Set final positions
        newImage1.rectTransform.position = isEntering ? image1TargetPosition : image1InitialPosition;
        newImage2.rectTransform.position = isEntering ? image2TargetPosition : image2InitialPosition;

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //KEYHOLE
    //==============================

    public static void Keyhole(Keyhole keyholePrefab, Transform targetTransform, Color colorTint, float time, AnimationCurve animCurve, MonoBehaviour runOnInstance, bool isEntering = true)
    {
        if (!effectActive)
        {
            currentlyRunningEffectOnInstance = runOnInstance;
            Initialise();
            Clear();
            effectActive = true;
            runOnInstance.StartCoroutine(KeyholeCor(keyholePrefab, targetTransform, colorTint, time, animCurve, isEntering));
        }
        else
        {
            Debug.LogWarning("Keyhole() called by " + runOnInstance + " but an effect is already running on " + currentlyRunningEffectOnInstance + ".");
        }
    }

    private static IEnumerator KeyholeCor(Keyhole keyholePrefab, Transform targetTransform, Color colorTint, float time, AnimationCurve animCurve, bool isEntering)
    {
        //Play SFX here

        //Get screen extents
        Vector2 extents = new Vector2(targetCanvas.pixelRect.width, targetCanvas.pixelRect.height);

        //Instantiate keyhole
        Keyhole keyHole = Instantiate(keyholePrefab, targetCanvas.transform);

        //Position keyhole centered over target
        Vector2 viewportPosition = Camera.main.WorldToViewportPoint(targetTransform.position);
        Vector2 screenPos = new Vector2((viewportPosition.x * extents.x) - (extents.x * 0.5f), (viewportPosition.y * extents.y) - (extents.y * 0.5f));
        keyHole.centre.rectTransform.localPosition = screenPos;

        //Set color tint
        SetImageColor(keyHole.fillerBL, colorTint);
        SetImageColor(keyHole.fillerB, colorTint);
        SetImageColor(keyHole.fillerBR, colorTint);

        SetImageColor(keyHole.fillerL, colorTint);
        SetImageColor(keyHole.centre, colorTint);
        SetImageColor(keyHole.fillerR, colorTint);

        SetImageColor(keyHole.fillerTL, colorTint);
        SetImageColor(keyHole.fillerT, colorTint);
        SetImageColor(keyHole.fillerTR, colorTint);

        //Get largest horizontal distance and largest vertical distance from centre of keyhole to edge of screen
        float maxXDist = Mathf.Max(extents.x - keyHole.centre.rectTransform.anchoredPosition.x, keyHole.centre.rectTransform.anchoredPosition.x);
        float maxYDist = Mathf.Max(extents.y - keyHole.centre.rectTransform.anchoredPosition.y, keyHole.centre.rectTransform.anchoredPosition.y);

        //maxDist is the largest distance from the centre of the keyhole to the edge of the screen in the directions up, down, left, right
        float maxDist = Mathf.Max(maxXDist, maxYDist);

        //Get maxWidth of keyhole as twice largest distance to edge
        float maxWidth = 2 * maxDist;
            
        //Determine initial and final keyhole sizes
        Vector2 initialSizeDelta = isEntering ? new Vector2(maxWidth, maxWidth) : Vector2.zero;
        Vector2 finalSizeDelta = isEntering ? Vector2.zero : new Vector2(maxWidth, maxWidth);

        //Compute magnitude of change
        float changeInSizeDeltaMagnitude = (finalSizeDelta - initialSizeDelta).magnitude;

        //Set initial size
        keyHole.centre.rectTransform.sizeDelta = initialSizeDelta;

        //Get total animation curve time, will be normalized to fit into time parameter
        float totalCurveTime = animCurve.keys[animCurve.length - 1].time;

        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            //Change size of keyhole, scaled by animation curve
            keyHole.centre.rectTransform.sizeDelta = Vector2.MoveTowards(initialSizeDelta, finalSizeDelta, changeInSizeDeltaMagnitude * animCurve.Evaluate(totalCurveTime * elapsedTime / time));

            //Compute distances to edges of screen
            float distToRightEdge = Mathf.Max(extents.x - keyHole.centre.rectTransform.anchoredPosition.x - keyHole.centre.rectTransform.sizeDelta.x * 0.5f, 0);
            float distToLeftEdge = Mathf.Max(keyHole.centre.rectTransform.anchoredPosition.x - keyHole.centre.rectTransform.sizeDelta.x * 0.5f, 0);
            float distToTopEdge = Mathf.Max(extents.y - keyHole.centre.rectTransform.anchoredPosition.y - keyHole.centre.rectTransform.sizeDelta.y * 0.5f, 0);
            float distToBottomEdge = Mathf.Max(keyHole.centre.rectTransform.anchoredPosition.y - keyHole.centre.rectTransform.sizeDelta.y * 0.5f, 0);

            //Fill rest of screen with filler
            keyHole.fillerBL.rectTransform.sizeDelta = new Vector2(distToLeftEdge, distToBottomEdge);
            keyHole.fillerB.rectTransform.sizeDelta = new Vector2(keyHole.fillerB.rectTransform.sizeDelta.x, distToBottomEdge);
            keyHole.fillerBR.rectTransform.sizeDelta = new Vector2(distToRightEdge, distToBottomEdge);

            keyHole.fillerL.rectTransform.sizeDelta = new Vector2(distToLeftEdge, keyHole.fillerB.rectTransform.sizeDelta.x);
            keyHole.fillerR.rectTransform.sizeDelta = new Vector2(distToRightEdge, keyHole.fillerB.rectTransform.sizeDelta.x);

            keyHole.fillerTL.rectTransform.sizeDelta = new Vector2(distToLeftEdge, distToTopEdge);
            keyHole.fillerT.rectTransform.sizeDelta = new Vector2(keyHole.fillerB.rectTransform.sizeDelta.x, distToTopEdge);
            keyHole.fillerTR.rectTransform.sizeDelta = new Vector2(distToRightEdge, distToTopEdge);

            //Advance time
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (isEntering)
        {
            //Replace with solid color
            keyHole.gameObject.SetActive(false);
            GameObject gameObject = new GameObject();
            gameObject.transform.parent = targetCanvas.transform;
            RawImage image = gameObject.AddComponent<RawImage>();
            image.rectTransform.localPosition = Vector2.zero;
            SetImageColor(image, colorTint);
            SetImageSize(image, extents.x, extents.y);
        }
        else
        {
            Destroy(keyHole.gameObject);
        }

        //Turn off effect
        effectActive = false;
    }

    //==============================
    //GENERAL METHODS
    //==============================

    private static RawImage CreateImagePrefab()
    {
        GameObject gameObject = new GameObject
        {
            name = "Image Prefab"
        };
        if (targetCanvas != null)
        {
            gameObject.transform.parent = targetCanvas.transform;
            gameObject.SetActive(false);
        }
        
        return gameObject.AddComponent<RawImage>();
    }

    private static void SetImageColor(RawImage rawImage, Color color)
    {
        rawImage.color = color;
    }

    private static void SetImageColor(Image rawImage, Color color)
    {
        rawImage.color = color;
    }

    private static void SetImageSize(RawImage rawImage, float width, float height)
    {
        rawImage.rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void SetImageAnchor(RawImage rawImage, bool left, bool bottom)
    {
        rawImage.rectTransform.anchorMin = new Vector2(left ? 0 : 1, bottom ? 0 : 1);
        rawImage.rectTransform.anchorMax = new Vector2(left ? 0 : 1, bottom ? 0 : 1);
    }

    private static void SetImagePivot(RawImage rawImage, float pivotX, float pivotY)
    {
        rawImage.rectTransform.pivot = new Vector2(pivotX, pivotY);
    }

    private static IEnumerator EnterEffect(RawImage rawImage)
    {
        float elapsedTime = 0f;
        while (elapsedTime < 0.3f)
        {
            rawImage.rectTransform.localScale = Vector2.Lerp(Vector2.zero, Vector2.one, elapsedTime / 0.3f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rawImage.rectTransform.localScale = Vector2.one;
    }

    private static List<Vector2Int> DiagonallyTileRectangle(int xMax, int yMax)
    {
        List<Vector2Int> diagonalCoords = new List<Vector2Int>();

        //x and y coordinates
        int x = 1;
        int y = 1;

        //Add first coordinate
        diagonalCoords.Add(new Vector2Int(x, y));

        //Counts numer of coordinates
        int i = 1;

        //Total number of coords
        int numCoords = xMax * yMax;

        while (i < numCoords)
        {
            //First step is to the right, e.g. (1, 1) --> (2, 1)
            //If not at far right
            if (x != xMax)
            {
                //Step forward
                x++;
            }
            else
            {
                //Otherwise if not at top
                if (y != yMax)
                {
                    //Step up
                    y++;
                }
                else
                {
                    //At top right, so stop
                    break;
                }
            }

            //Increment count
            i++;

            //Add next coordinate
            diagonalCoords.Add(new Vector2Int(x, y));

            //If found all coords, stop
            if (i == numCoords)
            {
                break;
            }

            //After stepping right, move diagonally back and up, e.g. (2, 1) --> (1, 2)
            //While midway through a row, and not at the top
            while (x > 1 && y < yMax && i < numCoords)
            {
                //Step up
                y++;

                //And back
                x--;

                //Increment count
                i++;

                //Add next coordinate
                diagonalCoords.Add(new Vector2Int(x, y));
            }

            //If found all coords, stop
            if (i == numCoords)
            {
                break;
            }

            //After moving diagonally backwards, want to step up, e.g. (1, 2) --> (1, 3)
            //If not at top
            if (y != yMax)
            {
                //Step up
                y++;
            }
            else
            {
                //Otherwise if not at far right
                if (x != xMax)
                {
                    //Step right
                    x++;
                }
                else
                {
                    //At top right, so stop
                    break;
                }
            }

            //Increment count
            i++;

            //Add next coordinate
            diagonalCoords.Add(new Vector2Int(x, y));

            //If found all coords, stop
            if (i == numCoords)
            {
                break;
            }

            //After stepping up, move diagonally down and right, e.g. (3, 1) --> (2, 2)
            //While midway through a column, and not at the far right
            while (y > 1 && x < xMax && i < numCoords)
            {
                //Step down
                y--;

                //Step forward
                x++;

                //Increment count
                i++;

                //Add next coordinate
                diagonalCoords.Add(new Vector2Int(x, y));
            }
        }

        return diagonalCoords;
    }
}