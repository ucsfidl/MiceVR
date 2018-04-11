function videoCompress(vname, outType, maxFrames, fps, scale, lowVal, highVal, deleteOriginal)
% Compress a video to MPEG4.
% This is run every night on the computer.
% Normal values are
% fps = 60
% scale = 0.5
% lowVal = 0
% highVal = 0.25
tic

vin = VideoReader(vname);

vout = VideoWriter([vname(1:end-4) '_out'], outType);
vout.FrameRate = fps;
%vout.Quality = 10;
open(vout);

cnt = 0;

while hasFrame(vin) && cnt < maxFrames
    vFrame = readFrame(vin);
    vFrame= imadjust(vFrame, [lowVal highVal]);
    vFrame = imresize(vFrame, scale);
    writeVideo(vout, vFrame);
    cnt = cnt + 1;
end

close(vout);

if deleteOriginal
   delete(vname); 
end

toc
end