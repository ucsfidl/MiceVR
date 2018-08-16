function [w h] = getScreenDim()

set(0,'units','pixels');
pixSS = get(0,'screensize');
w = pixSS(3);
h = pixSS(4);

end