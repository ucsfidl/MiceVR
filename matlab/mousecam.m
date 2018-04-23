function x = mousecam(fname)

imaqreset;

global trialStarts;
global trialEnds;
global lastEventTime;
global fps;

numCams = 2;

trialStarts = struct([]);
trialEnds = struct([]);
lastEventTime = 0;

if isempty(gcp('nocreate'))
    parpool(numCams)
end

delete(imaqfind);
spmd(numCams)
    delete(imaqfind);
end
   
% create video objects
spmd(numCams)
    for idx=1:numlabs % number of workers
        if idx == labindex
            imaqreset
            % Not sure what this does exactly, except it was in sample code, so disable.
            % Configure acquisition to not stop if dropped frames occur
            %imaqmex('feature', '-gigeDisablePacketResend', true);
            
            % Detect cameras
            uin = imaqhwinfo('gentl');
            numCamerasFound = numel(uin.DeviceIDs);
            fprintf('Worker %d detected %d cameras.\n', ...
                labindex, numCamerasFound);
        end
        labBarrier
    end
    cameraID = labindex;
    v = videoinput('gentle', cameraID, 'Mono8');
    s = v.Source;

    v.FramesPerTrigger = 1;
    v.LoggingMode = 'disk';
    v.ReturnedColorspace = 'grayscale';
    v.TriggerRepeat = Inf;
    v.ROIPosition = [440, 362, 400, 300];
    %vid{i}.ROIPosition = [240, 212, 800, 600];

    s.ExposureTime = 14000;  %15000 might be too fast
    s.AcquisitionFrameRateMode = 'Off';  
    % Keep this commented OUT, else lots of dropped frames!
    %s.AcquisitionFrameRateMode = 'Basic';  
    %s.AcquisitionFrameRate = 60;
    
    vw = VideoWriter(strcat(fname, '_', num2str(cameraID), '.avi'), 'Grayscale AVI');
    vw.FrameRate = 60;
    fps = vw.FrameRate;
    v.DiskLogger = vw;
    
    % Only set 1 trigger function, no need to write 2x
    if (cameraID == 1)
        v.TriggerFcn = {'logTrialMarks'};
    end
    
    preview(v); 

end

x = input('Press ENTER to stop preview & start recording with hardware trigger');

spmd(numCams)
    stoppreview(v);
    triggerconfig(v, 'hardware');
    % Uncomment if you want to test whether the display is really rendering
    % all frames!
%    if (i == 1)
%        triggerconfig(v, 'hardware');
%    else 
%        triggerconfig(v, 'immediate');
%    end
    s.TriggerMode = 'On';    
    s.TriggerActivation = 'RisingEdge';
    s.TriggerDelay = 0;
    s.TriggerSelector = 'FrameStart';
    s.TriggerSource = 'Line0';

    % Setup the feedback to send a signal each time a frame is triggered,
    % so that Unity can log the time associated with the signal to find the
    % frames in which a new trial started for eyetracking analysis.
    % NEED PULLUP RESISTOR!
    %src.LineSelector = 'Line1';
    %src.LineMode = 'Output';
    %src.LineSource = 'FrameTriggerWait';    
    %src.OutputDurationMode = 'On';
    %src.OutputDurationTime = 909;
    
    start(v);    
end


% Uncomment if you want to test whether the display is really rendering
% all frames!  It was not!  So frames were not actually dropped!
%vid{2}.FramesPerTrigger = Inf;
%src2 = getselectedsource(vid{2});
%src2.ExposureTime = 3500;

x = input('Press ENTER to stop recording');

framesAcquiredLogged = zeros(n);

spmd(numCameras)
    stop(v);
    framesAcquiredLogged(i,1) = vid{i}.FramesAcquired;
    framesAcquiredLogged(i,2) = vid{i}.DiskLoggerFrameCount;

    % Display number of frames acquired and logged while acquiring
    while strcmp(v.Logging, 'on')
        disp([v.FramesAcquired , v.DiskLoggerFrameCount])
        pause(1)
    end
    
    % Wait until acquisition is complete and specify wait timeout
    wait(v, 100);

    % Wait until all frames are logged
    while (v.FramesAcquired ~= v.DiskLoggerFrameCount) 
        pause(1);
    end
    disp([v.FramesAcquired v.DiskLoggerFrameCount]);    
end

save(fname, 'trialStarts', 'trialEnds', 'framesAcquiredLogged');

imaqreset

spmd(numCameras)
    delete(imaqfind);
end

close all;
end