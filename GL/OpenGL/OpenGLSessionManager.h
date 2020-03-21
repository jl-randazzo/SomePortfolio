#ifndef GRAPHICSUTILITIES_H
#define GRAPHICSUTILITIES_H

#define ER_CONTEXT_DOES_NOT_EXIST -1
#define ER_CONTEXT_ALREADY_EXISTS -2

#include<map>
#include<utility>
#include<QBasics.h>
#include <QOpenGLWindow>
#include <QDragEnterEvent>
#include <QOpenGLFunctions>
#include <QtOpenGL>
#include <QOpenGLTexture>

int getSizeForGLenumType(GLenum type);

struct ProgramShaderHandles {
    ProgramShaderHandles(GLuint prog, GLuint vert, GLuint frag): prog_handle(prog), vert_handle(vert), frag_handle(frag){}
    const GLuint prog_handle;
    const GLuint vert_handle;
    const GLuint frag_handle;
};

struct NamePathPair{
public:
    NamePathPair(QString name, QString path): name(name), path(path){}
    NamePathPair(){}
    const QString name;
    const QString path;
};

struct VertFragShaderData{
    VertFragShaderData(QString name, NamePathPair vert, NamePathPair frag): name(name), vert(vert), frag(frag){}
    VertFragShaderData(QString name): name(name){}
    const QString name;
    const NamePathPair vert;
    const NamePathPair frag;
};

class OpenGLSessionManager {
public:
    static void buildSession(QOpenGLContext* context);
    static OpenGLSessionManager * getInstance();
    const QOpenGLContext * getContext() const;
    QOpenGLTexture const* createOpenGLTexture(const QImage *image);
    GLuint useProgram(QString name) const;
    void setUniform2DTextureValue(GLuint prog_id, const char * name, GLuint tex_id, GLint index, GLenum tex_enum) const;
    void configureAttributesAndDraw(const GLenum draw_type, size_t attribute_count, const int * attribute_sizes, const GLenum * types, size_t vertex_count, const void * data) const;

private:
    OpenGLSessionManager(QOpenGLContext const* context);
    void loadShaderPrograms();
    GLuint getCompiledShaderHandle(NamePathPair name_path_pair, GLenum shader_type) const;
    void logShaderCompilationError(QString shader_name, GLuint shader_handle) const;
    void logProgramLinkError(QString prog_name, GLuint prog_handle) const;

    // instance variables
    QOpenGLContext const* context;
    std::map<QString, ProgramShaderHandles>* shader_map;

    static OpenGLSessionManager* instance;
};


#endif // GRAPHICSUTILITIES_H
