function x = mousecam(fname)

imaqreset;

vid = cell(1,2);

n = 2;

for i=1:n
    vid{i} = videoinput('gentl', i, 'Mono8');  % Change 1 to 2 if wrong camera previewed
    src = getselectedsource(vid{i});
    
    vid{i}.FramesPerTrigger = 1;
    vid{i}.LoggingMode = 'disk';
    vid{i}.ReturnedColorspace = 'grayscale';
    vid{i}.TriggerRepeat = Inf;
    vid{i}.ROIPosition = [240, 212, 800, 600];

    src.ExposureTime = 14000;  %15000 might be too fast
    src.AcquisitionFrameRateMode = 'Off';  
    % Keep this commented OUT, else lots of dropped frames!
    %src.AcquisitionFrameRateMode = 'Basic';  
    %src.AcquisitionFrameRate = 60;

    vid{i}.DiskLogger = VideoWriter(strcat(fname, '_', num2str(i), '.avi'), 'Grayscale AVI');

    preview(vid{i}); 
end

% Uncomment if you want to test whether the display is really rendering
% all frames!  It was not!  So frames were not actually dropped!
%vid{2}.FramesPerTrigger = Inf;
%src2 = getselectedsource(vid{2});
%src2.ExposureTime = 3500;

x = input('Press ENTER to stop preview & make available for hardware trigger');

for i=1:n
    stoppreview(vid{i});
    triggerconfig(vid{i}, 'hardware');
    % Uncomment if you want to test whether the display is really rendering
    % al frames!
%    if (i == 1)
%        triggerconfig(vid{i}, 'hardware');
%    else 
%        triggerconfig(vid{i}, 'immediate');
%    end
    src = getselectedsource(vid{i});
    src.TriggerMode = 'On';    
    src.TriggerActivation = 'RisingEdge';
    src.TriggerDelay = 0;
    src.TriggerSelector = 'FrameStart';
    src.TriggerSource = 'Line0';

    start(vid{i});
end

x = input('Press ENTER to stop recording');

log = cell(1,2);
for i=1:n
    stop(vid{i});
    log{i} = vid{i}.EventLog;    
end

save('data', 'log');

close all;
end