function plotFilledEllipses(x, y, widthX, widthY, color)

alpha = -pi:0.01:pi;
a = cos(alpha);
b = sin(alpha);

for i=1:length(x)
    fill(x(i) - a * widthX(i), y(i) - b * widthY(i), color, 'LineStyle', 'none');
end


end