#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QDropEvent>

QT_BEGIN_NAMESPACE
namespace Ui { class MainWindow; }
QT_END_NAMESPACE

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

private slots:
    void on_actionImport_Sprites_triggered();
    void on_actionUndo_triggered();
    void update();

    void on_displayGray_clicked();

    void on_genGrayScale_clicked();

    void on_displayOriginal_clicked();

    void on_displayLines_clicked();

    void on_displaySobel_clicked();

    void on_displayErosion_clicked();

    void on_displayDepop_clicked();

    void on_displayCandidates_clicked();

private:
    Ui::MainWindow *ui;
    QOpenGLContext *glContext;
    QTimer *updateTimer;
};
#endif // MAINWINDOW_H
