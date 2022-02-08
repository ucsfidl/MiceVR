function trackPupilsAuto(mouseName, day, rig, startingOtsuWeights)
% This is a wrapper of the trackPupils function that will try to find the best otsuWeight parameter, that is,
% the one that minimizes (a) the overall pupil movement, and (b) the number of frames without a 
% pupil detected.  Large "pupil" movements and many frames without pupil tracked is indicative of poor tracking, 
% so the program will try another otsuWeight parameter.

% TODO: Consider using changes in axis length as well as center jumps, as what often changes more is axis length
% but not center position, at least by not as much.

stepSize = 0.02;
frameLimPrelim = [1 1000];
crPresent = [1 1];
frameLimFull = [1 0];
maxOtsuWeight = 0.6;

numOtsuSteps = [floor((maxOtsuWeight - startingOtsuWeights(1)) / stepSize); ...
            floor((maxOtsuWeight - startingOtsuWeights(2)) / stepSize)];

rigPxPerMm = loadRigPxPerMm();

currRigPxPerMm = rigPxPerMm(rig,:);
calibWeights = currRigPxPerMm / 50;

minAcceptablePupilMovementFraction = 0.7 * calibWeights;  % E.g. 50 px / 100 frames - TODO: update based on rig px/mm calibration
minAcceptableUntrackedFramesFraction = 0.1; % 10% untracked is acceptable, more is not

currentOtsuWeights = startingOtsuWeights;

leftEyeFractions = nan(numOtsuSteps(1), 2);  % movementFraction, untrackedFramesFraction
rightEyeFractions = nan(numOtsuSteps(2), 2);

vLeftNum = 1;
vRightNum = 2;
if (rig == 1)
    vLeftNum = 2;
    vRightNum = 1;
end

if (day < 10)
    dayStr = ['00' num2str(day)];
elseif (day < 100)
    dayStr = ['0' num2str(day)];
else
    dayStr = num2str(day);
end
vFileName = [mouseName '_' dayStr];
trackFileName = [vFileName '_part_trk.mat'];

minError = 1;

lastTestFrame = frameLimPrelim(2);
numFramesPrelim = frameLimPrelim(2) - frameLimPrelim(1) + 1;

while (true)
    sumPupilMovement = zeros(1,2);  % Calculate the pupil movement for each eye separately, to update otsuWidth.
    numFramesWithoutPupil = zeros(1,2);  % Keep track of missing pupil frames, as we will minimize that
    lastValidPupilCenter = -ones(1,2);
    
    trackPupils(vFileName, vLeftNum, vRightNum, frameLimPrelim, currentOtsuWeights, crPresent);
    load(trackFileName, 'centers');
    for eye = 1:2
        numFramesWithoutPupil(eye) = sum(isnan(centers(:,1,eye)));
        for k=1:lastTestFrame
            if (~isnan(centers(k,1,eye)))
                lastValidPupilCenter = centers(k,:,eye);
                break;
            end
        end
        for k=k+1:lastTestFrame
            if (~isnan(centers(k,1,eye)))
                sumPupilMovement(eye) = sumPupilMovement(eye) + norm(lastValidPupilCenter - centers(k,:,eye));
                lastValidPupilCenter = centers(k,:,eye);
            end
        end
    end
    
    leftIdx = find(isnan(leftEyeFractions), 1);
    if (~isempty(leftIdx)) % The memory array hasn't been filled up, so continue writing to it
        leftEyeFractions(leftIdx,:) = [sumPupilMovement(1) / numFramesPrelim; ...
                                        numFramesWithoutPupil(1) / numFramesPrelim];
    end
    rightIdx = find(isnan(rightEyeFractions), 1);
    if (~isempty(rightIdx)) % The memory array hasn't been filled up, so continue writing to it
        rightEyeFractions(rightIdx,:) = [sumPupilMovement(2) / numFramesPrelim; ...
                                        numFramesWithoutPupil(2) / numFramesPrelim];
    end
    
    disp(['Results for weights [' num2str(currentOtsuWeights(1)) ' ' num2str(currentOtsuWeights(2)) ']: ' ...
            num2str(leftEyeFractions(leftIdx, 1)) ' - ' num2str(leftEyeFractions(leftIdx, 2)) ' | ' ...
            num2str(rightEyeFractions(rightIdx, 1)) ' - ' num2str(rightEyeFractions(rightIdx, 2))]);
    
    currentOtsuWeights = currentOtsuWeights + stepSize;
    if (currentOtsuWeights(1) > maxOtsuWeight)
        currentOtsuWeights(1) = maxOtsuWeight;
    end
    if (currentOtsuWeights(2) > maxOtsuWeight)
        currentOtsuWeights(2) = maxOtsuWeight;
    end
    
    if (sum(currentOtsuWeights == maxOtsuWeight) == 2)
        break;
    end
    
    %{
    updateOtsuWeights = zeros(1,2);
    
    for eye=1:2
        if (sumPupilMovement(eye) / numFramesPrelim > minAcceptablePupilMovementFraction(eye) || ...
                numFramesWithoutPupil(eye) / numFramesPrelim > minAcceptableUntrackedFramesFraction)
            updateOtsuWeights(eye) = 1;
            disp(['Eye ' num2str(eye) ' otsuWeight of ' num2str(currentOtsuWeights(eye)) ' rejected: ' ...
                num2str(sumPupilMovement(eye) ./ numFramesPrelim) ' | ' ...
                num2str(numFramesWithoutPupil(eye) ./ numFramesPrelim)]);
            currentOtsuWeights(eye) = currentOtsuWeights(eye) + stepSize;
        else
            disp(['Eye ' num2str(eye) ' otsuWeight of ' num2str(currentOtsuWeights(eye)) ' ACCEPTED: ' ...
                num2str(sumPupilMovement(eye) ./ numFramesPrelim) ' | ' ...
                num2str(numFramesWithoutPupil(eye) ./ numFramesPrelim)]);
        end
    end
    % If either eye needs to be updated, update OtsuWeight.  If not, run full trackPupils with current weights
    if (sum(updateOtsuWeights) == 0 || max(currentOtsuWeights) > maxOtsuWeight)
        disp ('Starting tracking of entire videos');
        trackPupils(vFileName, vLeftNum, vRightNum, frameLimFull, currentOtsuWeights, crPresent);
        break;
    end
    %}
end

%minLeftIdx = find(min(sum(leftEyeFractions,2)) == sum(leftEyeFractions,2), 1);
% Pick the otsuWeight which has the minimum Pupil displacement when at most 10% of frames lack a tracked pupil
% This works better than the commented code above
candLeftIdx = find(leftEyeFractions(:,2) <= minAcceptableUntrackedFramesFraction);
candLeftEyeMovements = leftEyeFractions(candLeftIdx, 1);
minLeftIdx = candLeftIdx(find(candLeftEyeMovements == min(candLeftEyeMovements), 1));
leftOtsuWeight = (minLeftIdx - 1) * stepSize + startingOtsuWeights(1);

%minRightIdx = find(min(sum(rightEyeFractions,2)) == sum(rightEyeFractions,2), 1);
candRightIdx = find(rightEyeFractions(:,2) <= minAcceptableUntrackedFramesFraction);
candRightEyeMovements = rightEyeFractions(candRightIdx, 1);
minRightIdx = candRightIdx(find(candRightEyeMovements == min(candRightEyeMovements), 1));
rightOtsuWeight = (minRightIdx - 1) * stepSize + startingOtsuWeights(2);

disp (['Starting tracking of entire videos with otsuWeights = [' num2str(leftOtsuWeight) ' ' num2str(rightOtsuWeight)]);
trackPupils(vFileName, vLeftNum, vRightNum, frameLimFull, [leftOtsuWeight rightOtsuWeight], crPresent);

end