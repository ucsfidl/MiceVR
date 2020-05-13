function mousecamCalib(mouseName, eye, numCams, triggerType)

% This function takes a mouse's name and records the inputs from 2 eye
% cameras to disk.

% As of Oct 2019, it now also checks the google sheet to read the day from
% there, so trainers no longer have to input the day.

% It uses the Parallel Computing Toolbox, as I found that this was the best
% way to ensure that frames are not dropped while recording from 2 cameras
% at the same time.

global trialStarts;
global trialEnds;
global lastEventTime;
global fps;

trialStarts = struct([]);
trialEnds = struct([]);
lastEventTime = 0;
fps = 60;

% vrGSdocidFileName = '../config/vrGSdocid.txt';
DAY = 1;

vidWidth = 208;  % 200
vidHeight = 150; % 150

numSlaves = numCams - 1;

imaqreset
delete(imaqfind);

if (numSlaves > 0)
    if isempty(gcp('nocreate'))
        parpool(numSlaves)
    end

    spmd(numSlaves)
        delete(imaqfind);
    end
end

% First, using the Google Sheet ID specified as a variable above, read the
% Google Sheet and find the first of the last days without a result.
vidFileName = mouseName;
%{
if (isfile(vrGSdocidFileName)) % If the docid file exists, use that to find the Google Sheet for this mouse
    fid = fopen(vrGSdocidFileName);
    docid = fgetl(fid);
    sheetID = GetSheetIDs(docid, {mouseName}, 1);   % Data sheets only, not all sheets
    sheet = GetGoogleSpreadsheet(docid, sheetID);
    lastDay = '0';
    prefix = '';
    for row=1:size(sheet, 1)
        d = sheet{row, DAY};
        if (isstrprop(d, 'digit'))  % Found a day, store it overwriting the last one
            lastDay = d;
        end
    end
    if (str2double(lastDay) < 10)
        prefix = '00';
    elseif (str2double(lastDay) < 100)
        prefix = '0';
    end
    vidFileName = [vidFileName '_' prefix lastDay];
end
%}

% Second, run in non-parellel mode to get the image previews needed for
% making sure the eyes are in focus and lighting adujustments.  
% Then, below, do the actual recording using 2 separate processes, 1 for each camera.

% vid{1} will be used to record videos in the current thread, while vid{2}
% will be closed and its camera recording will occur on the slave thread.
vid = cell(numCams,1);

for i=1:numCams
    camID = i;
    if (numCams == 1)
        if (eye == 'L')
            camID = 1;
        else
            camID = 2;
        end
    end
    vid{i} = videoinput('gentl', camID, 'Mono8');
    src = getselectedsource(vid{i});
    
    vid{i}.FramesPerTrigger = 1;
    vid{i}.LoggingMode = 'disk';
    vid{i}.ReturnedColorspace = 'grayscale';
    vid{i}.TriggerRepeat = Inf;
    
    if (strcmp(src.DeviceVendorName,'Basler'))
        src.BinningHorizontal = 2;
        src.BinningVertical = 2;
        vid{i}.ROIPosition = [220, 181, vidWidth, vidHeight];
        %vid{i}.ROIPosition = [110, 90, vidWidth, vidHeight];
    else
        vid{i}.ROIPosition = [540, 437, vidWidth, vidHeight];
    end
    
    src.ExposureTime = 14000;  %15000 might be too fast
    %if src.DeviceVendorName ~= 'Basler'
    %    src.AcquisitionFrameRateMode = 'Off';
    %end
    % Keep this commented OUT, else lots of dropped frames!
    %src.AcquisitionFrameRateMode = 'Basic';  
    %src.AcquisitionFrameRate = 60;
    
    vw = VideoWriter(strcat(vidFileName, '_', num2str(i), '.mp4'), 'MPEG-4');
    vw.FrameRate = 60;
    fps = vw.FrameRate;
    vid{i}.DiskLogger = vw;
    
    vid{i}.TriggerFcn = {'logTrialMarks'};

    hImage = image(zeros(vidHeight, vidWidth, 1));
    h = preview(vid{i}, hImage); 

    % Overlay a target so I can align the camera onto the central point of corneal curvature
    hold on;
    for j=1:16
        plot(j*[vidWidth vidWidth]/16, [0 vidHeight]);
    end
    
    if (i == 2)
        movegui(h, [1570 1720]);
        movegui(h);
        %set(h, 'Position', [h.Position(1) h.Position(2) h.Position(3)+vidWidth*1.2 h.Position(4)]);
    end
end

x = input('Press ENTER to stop preview & start recording');

for i=1:numCams
    % I want to keep seeing the preview while it records, so commented out
    %stoppreview(vid{i});
    if (i ~= 1)
        delete(vid{i});
    end
end

% Now, setup the master thread which will record 1 camera
if (triggerType == 'h')
    triggerconfig(vid{1}, 'hardware');
    src = getselectedsource(vid{1});
    src.TriggerMode = 'On';    
    src.TriggerActivation = 'RisingEdge';
    src.TriggerDelay = 0;
    src.TriggerSelector = 'FrameStart';
    if (strcmp(src.DeviceVendorName,'Basler'))
        src.TriggerSource = 'Line3';        
    else
        src.TriggerSource = 'Line0';    
    end
elseif (triggerType == 'i')
    triggerconfig(vid{1}, 'immediate');
else
    error('invalid 3rd arg');
end

start(vid{1});

% Next, setup parallel recording, 1 process per cameras after the first camera
if (numSlaves > 0)
    spmd(numSlaves)
        for idx=1:numlabs % number of workers
            if idx == labindex
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
        v = videoinput('gentl', cameraID, 'Mono8');
        s = v.Source;

        v.FramesPerTrigger = 1;
        v.LoggingMode = 'disk';
        v.ReturnedColorspace = 'grayscale';
        v.TriggerRepeat = Inf;
        if (strcmp(src.DeviceVendorName,'Basler'))
            s.BinningHorizontal = 2;
            s.BinningVertical = 2;
            v.ROIPosition = [220, 181, vidWidth, vidHeight];
            %v.ROIPosiion = [110, 90, vidWidth, vidHeight];
        else
            v.ROIPosition = [540, 437, 200, 150];        
        end

        s.ExposureTime = 14000;  %15000 might be too fast
        %if src.DeviceVendorName ~= 'Basler'
        %    s.AcquisitionFrameRateMode = 'Off';  
        %end
        % Keep this commented OUT, else lots of dropped frames!
        %s.AcquisitionFrameRateMode = 'Basic';  
        %s.AcquisitionFrameRate = 60;

        vw = VideoWriter(strcat(vidFileName, '_', num2str(cameraID+1), '.mp4'), 'MPEG-4');
        vw.FrameRate = fps;
        v.DiskLogger = vw;

        % preview(v);  % Does not work for parallel processing toolbox
    end

    spmd(numSlaves)
        if (triggerType == 'h')
            triggerconfig(v, 'hardware');

            s.TriggerMode = 'On';    
            s.TriggerActivation = 'RisingEdge';
            s.TriggerDelay = 0;
            s.TriggerSelector = 'FrameStart';
            if (strcmp(src.DeviceVendorName,'Basler'))
                s.TriggerSource = 'Line3';
            else
                s.TriggerSource = 'Line0';        
            end
        elseif (triggerType == 'i')
            triggerconfig(v, 'immediate');
        else
            error('invalid 3rd arg');
        end

        start(v);
    end
end

x = input('Press ENTER to stop recording');

framesAcquiredLogged = zeros(1,2);
stop(vid{1});
framesAcquiredLogged(1,1) = vid{1}.FramesAcquired;
framesAcquiredLogged(1,2) = vid{1}.DiskLoggerFrameCount;

if (numSlaves > 0)
    spmd(numSlaves)
        stop(v);

        framesAcqLog = zeros(1, 2);
        framesAcqLog(1,1) = v.FramesAcquired;
        framesAcqLog(1,2) = v.DiskLoggerFrameCount;

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
        %disp([v.FramesAcquired v.DiskLoggerFrameCount]);    

        framesAcqLog = gcat(framesAcqLog, 1, 1);
    end
    framesAcquiredLogged = cat(1, framesAcquiredLogged, framesAcqLog{1});
end

disp(framesAcquiredLogged);

save(vidFileName, 'trialStarts', 'trialEnds', 'framesAcquiredLogged');

imaqreset

if (numSlaves > 0)
    spmd(numSlaves)
        delete(imaqfind);
    end
end

close all;
end