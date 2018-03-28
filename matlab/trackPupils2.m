function trackPupils(collageFileName, frameLim, fps, radLim1, edgeThr)
% This script will analyze a video file containing both eyes, the right eye
% on the left side and the left eye on the right side of the video. 
% It then saves several outputs:
%  - The centroids of each pupil over time
%  - The radius of each pupil over time
%  - The deviation in elevation of the pupil over time (extracted from
%  centroids - PCA?)
%  - The deviation in azimuth of the pupil over time
%  - A list of trial boundary times
%  - A list of eye blinks over time?
% It will also output graphs showing the deviation in azimuth over time for both eyes,
% the deviation in elevation over time for both eyes, and the deviation in
% both pupil sizes over time (5 traces per session).  The traces will have
% shading to indicate different trials.
% This program also produces an annotated video in which the pupils are
% outlined.

% Good settings for arguments:
% radLim = [15 36]
% edgeThr = 0

tic

frameStart = frameLim(1);
frameStop = frameLim(2);

vin = VideoReader(collageFileName);
currFrame = frameStart;

if frameStop == 0 || frameStop > vin.NumberOfFrames
    frameStop = vin.NumberOfFrames;
end

numFrames = frameStop - frameStart + 1;
vin = VideoReader(collageFileName); % Reopen because Matlab is lame
vin.CurrentTime = currFrame * 1/fps - 1/fps;

% Init storage variables
centers = zeros(numFrames, 2, 2); % Z dimension is 1 for each eye, left eye first
radii = zeros(numFrames, 1, 2);  % Z dimension is 1 for each eye, left eye first
elev = zeros(1, numFrames);
azim = zeros(1, numFrames);

vout = VideoWriter([collageFileName(1:end-4) '_ann.mp4'], 'MPEG-4');
vout.FrameRate = fps;
open(vout);

imLR = zeros(vin.Height, vin.Width/2, vin.BitsPerPixel/8, 2, 'uint8'); % 2 RGB images per video frame

% The collage file has 2 videos in it: right eye on left and left eye on
% right.  So split the image in 2 to analyze, in parallel, then stitch back
% together with annotation for confirmation of accurate tracking.

% Also, some bouncing motion needs to be accounted for with frame
% registration.

while currFrame <= frameStop
    im = readFrame(vin);
    for i=1:2  % 1 is L, 2 is R
        if (i == 1)
            imLR(:,:,:,i) = im(:, (size(im,2)/2 + 1):end, :);
        else
            imLR(:,:,:,i) = im(:, 1:(size(im,2)/2), :);
        end
        [c, r] = imfindcircles(imLR(:,:,:,i), radLim1, 'ObjectPolarity', 'dark', ...
            'Method', 'phasecode', 'EdgeThreshold', edgeThr);
        %[c1, r1] = imfindcircles(imLR(:,:,:,i), radLim1, 'ObjectPolarity', 'dark', ...
        %    'Method', 'phasecode', 'EdgeThreshold', edgeThr);
        %[c2, r2] = imfindcircles(imLR(:,:,:,i), radLim2, 'ObjectPolarity', 'dark', ...
        %    'Method', 'phasecode', 'EdgeThreshold', edgeThr);
        %c = cat(1, c1, c2);
        %r = cat(1, r1, r2);
        if (~isempty(c))
            idx = find(r == max(r));
            centers(currFrame,:,i) = c(idx, :);
            radii(currFrame,:,i) = r(idx);
            imLR(:,:,:,i) = insertShape(imLR(:,:,:,i), 'circle', [c(1,:) r(1)], ...
                'LineWidth', 1, 'Color', 'blue');
        else
            centers(currFrame,:,i) = [NaN NaN];
            radii(currFrame,:,i) = [NaN];
        end
    end
    
    join = cat(2, imLR(:,:,:,2), imLR(:,:,:,1));
    writeVideo(vout, join);
    
    currFrame = currFrame + 1;
end

close(vout);

toc

end

tic
im = readFrame(vin);
im1 = im(:,1:200,:);
bw1 = imbinarize(im1, 0.4*graythresh(im1));
bwc1 = imcomplement(bw1(:,:,1));
cc = bwconncomp(bwc1);
numPixels = cellfun(@numel, cc.PixelIdxList);
[biggest,idx] = max(numPixels);
newIm1 = zeros(size(bwc1));
newIm1(cc.PixelIdxList{idx}) = 1;
s = regionprops(newIm1, {'Centroid', 'MajorAxisLength', 'MinorAxisLength', 'Orientation', ...
    'ConvexArea'});

toc;
figure
imshow(newIm1);

t = linspace(0,2*pi,50);

hold on
for k = 1:length(s)
    a = s(k).MajorAxisLength/2;
    b = s(k).MinorAxisLength/2;
    Xc = s(k).Centroid(1);
    Yc = s(k).Centroid(2);
    phi = deg2rad(-s(k).Orientation);
    x = Xc + a*cos(t)*cos(phi) - b*sin(t)*sin(phi);
    y = Yc + a*cos(t)*sin(phi) + b*sin(t)*cos(phi);
    plot(x,y,'r','Linewidth',1)
end
hold off
